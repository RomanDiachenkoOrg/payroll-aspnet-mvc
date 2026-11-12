using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Payroll.Domain.Abstractions;
using Payroll.Domain.Common;
using Payroll.Domain.Enums;
using Payroll.Domain.Services;
using Payroll.Web.Infrastructure;
using Payroll.Web.Models;

namespace Payroll.Web.Controllers;

[Authorize]
public class TimesheetsController : Controller
{
    private readonly ITimesheetService _timesheets;
    private readonly IEmployeeService _employees;
    private readonly ICurrentUser _user;

    public TimesheetsController(ITimesheetService timesheets, IEmployeeService employees, ICurrentUser user)
    {
        _timesheets = timesheets;
        _employees = employees;
        _user = user;
    }

    public async Task<IActionResult> Index([FromQuery] TimesheetQuery query, CancellationToken ct)
    {
        var result = await _timesheets.SearchAsync(query, ct);
        return View(new TimesheetListViewModel
        {
            Query = query,
            Result = result,
            CanReview = _user.Role is UserRole.Admin or UserRole.Manager,
            ShowEmployee = _user.Role != UserRole.Employee
        });
    }

    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var timesheet = await _timesheets.GetAsync(id, ct);
        if (timesheet is null) return NotFound();
        if (!_timesheets.CanView(timesheet)) return Forbid();

        return View(new TimesheetDetailsViewModel
        {
            Timesheet = timesheet,
            CanEdit = _timesheets.CanEdit(timesheet),
            CanReview = _timesheets.CanReview(timesheet),
            Reject = new RejectTimesheetViewModel { Id = id }
        });
    }

    public async Task<IActionResult> Create(CancellationToken ct)
    {
        if (_user.Role != UserRole.Admin && _user.EmployeeId is null)
        {
            this.Error("Your account is not linked to an employee record.");
            return RedirectToAction(nameof(Index));
        }

        var weekStart = DateHelper.StartOfWeek(DateTime.Today);
        var model = new TimesheetFormViewModel
        {
            EmployeeId = _user.Role == UserRole.Admin ? null : _user.EmployeeId,
            WeekStart = weekStart
        };
        model.EnsureSevenDays(weekStart);
        for (var i = 0; i < 5; i++) model.Days[i].Hours = 8;
        await PopulateAsync(model, ct);
        return View("Edit", model);
    }

    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var timesheet = await _timesheets.GetAsync(id, ct);
        if (timesheet is null) return NotFound();
        if (!_timesheets.CanEdit(timesheet))
        {
            this.Error("This timesheet can no longer be edited.");
            return RedirectToAction(nameof(Details), new { id });
        }

        var model = new TimesheetFormViewModel
        {
            Id = timesheet.Id,
            EmployeeId = timesheet.EmployeeId,
            WeekStart = timesheet.WeekStart,
            Notes = timesheet.Notes,
            Status = timesheet.Status,
            RejectionReason = timesheet.RejectionReason,
            Days = timesheet.Entries.OrderBy(e => e.WorkDate)
                .Select(e => new DayHours { Date = e.WorkDate, Hours = e.Hours }).ToList()
        };
        model.EnsureSevenDays(timesheet.WeekStart);
        await PopulateAsync(model, ct);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Save(TimesheetFormViewModel model, string? submitAction, CancellationToken ct)
    {
        if (_user.Role != UserRole.Admin) model.EmployeeId = _user.EmployeeId;

        if (!ModelState.IsValid)
        {
            model.EnsureSevenDays(DateHelper.StartOfWeek(model.WeekStart ?? DateTime.Today));
            await PopulateAsync(model, ct);
            return View("Edit", model);
        }

        var input = new TimesheetInput(
            model.Id,
            model.EmployeeId!.Value,
            model.WeekStart!.Value,
            model.Days.Select(d => d.Hours ?? 0m).ToList(),
            model.Notes);

        var result = await _timesheets.SaveAsync(input, ct);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                var key = error.Field switch
                {
                    null => string.Empty,
                    var f when f.StartsWith("Hours[") => "Days[" + f[6..^1] + "].Hours",
                    var f => f
                };
                ModelState.AddModelError(key, error.Message);
            }
            model.EnsureSevenDays(DateHelper.StartOfWeek(model.WeekStart!.Value));
            await PopulateAsync(model, ct);
            return View("Edit", model);
        }

        var id = result.Value!.Id;
        if (submitAction == "submit")
        {
            var submit = await _timesheets.SubmitAsync(id, ct);
            if (submit.Succeeded) this.Success("Timesheet was saved and submitted for approval.");
            else this.Error(submit.ErrorMessage);
        }
        else
        {
            this.Success("Timesheet was saved as draft.");
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> Submit(int id, CancellationToken ct)
    {
        var result = await _timesheets.SubmitAsync(id, ct);
        if (result.Succeeded) this.Success("Timesheet was submitted for approval.");
        else this.Error(result.ErrorMessage);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Roles = Roles.ManagerOrAdmin)]
    public async Task<IActionResult> Approve(int id, string? returnUrl, CancellationToken ct)
    {
        var result = await _timesheets.ApproveAsync(id, ct);
        if (result.Succeeded) this.Success("Timesheet was approved.");
        else this.Error(result.ErrorMessage);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return LocalRedirect(returnUrl);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Roles = Roles.ManagerOrAdmin)]
    public async Task<IActionResult> Reject([Bind(Prefix = "Reject")] RejectTimesheetViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            this.Error(string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        var result = await _timesheets.RejectAsync(model.Id, model.Reason, ct);
        if (result.Succeeded) this.Success("Timesheet was rejected and returned to the employee.");
        else this.Error(result.ErrorMessage);
        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    private async Task PopulateAsync(TimesheetFormViewModel model, CancellationToken ct)
    {
        model.CanChooseEmployee = _user.Role == UserRole.Admin && model.Id is null;
        if (_user.Role == UserRole.Admin)
        {
            var employees = await _employees.GetActiveLookupAsync(null, ct);
            model.EmployeeOptions = employees.Select(e =>
                new SelectListItem($"{e.FullName} ({e.EmployeeNumber})", e.Id.ToString(), e.Id == model.EmployeeId));
        }
        else if (model.EmployeeId.HasValue)
        {
            var me = await _employees.GetAsync(model.EmployeeId.Value, false, ct);
            if (me is not null)
                model.EmployeeOptions = new[] { new SelectListItem(me.FullName, me.Id.ToString(), true) };
        }
    }
}
