using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Payroll.Domain.Abstractions;
using Payroll.Domain.Enums;
using Payroll.Domain.Services;
using Payroll.Web.Infrastructure;
using Payroll.Web.Models;

namespace Payroll.Web.Controllers;

[Authorize]
public class EmployeesController : Controller
{
    private readonly IEmployeeService _employees;
    private readonly IDepartmentService _departments;
    private readonly ISettingsService _settings;
    private readonly IPayRunService _payRuns;
    private readonly ICurrentUser _user;

    public EmployeesController(
        IEmployeeService employees,
        IDepartmentService departments,
        ISettingsService settings,
        IPayRunService payRuns,
        ICurrentUser user)
    {
        _employees = employees;
        _departments = departments;
        _settings = settings;
        _payRuns = payRuns;
        _user = user;
    }

    [Authorize(Roles = Roles.ManagerOrAdmin)]
    public async Task<IActionResult> Index([FromQuery] EmployeeQuery query, CancellationToken ct)
    {
        var result = await _employees.SearchAsync(query, ct);
        var model = new EmployeeListViewModel
        {
            Query = query,
            Result = result,
            Departments = await DepartmentOptionsAsync(query.DepartmentId, ct),
            CanManage = _user.Role == UserRole.Admin
        };
        return View(model);
    }

    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var isOwn = _user.EmployeeId == id;
        if (_user.Role == UserRole.Employee && !isOwn)
            return Forbid();

        var employee = await _employees.GetAsync(id, includeDetails: true, ct);
        if (employee is null) return NotFound();

        var finalisedOnly = _user.Role != UserRole.Admin;
        var payslips = (_user.Role == UserRole.Admin || isOwn)
            ? await _payRuns.GetPayslipsForEmployeeAsync(id, finalisedOnly, ct)
            : Array.Empty<Domain.Entities.Payslip>();

        return View(new EmployeeDetailsViewModel
        {
            Employee = employee,
            Payslips = payslips,
            CanManage = _user.Role == UserRole.Admin,
            IsOwnProfile = isOwn,
            Raise = new RaiseViewModel { EmployeeId = id }
        });
    }

    /// <summary>"My profile" for any signed-in user linked to an employee record.</summary>
    public IActionResult Me()
    {
        if (_user.EmployeeId is null)
        {
            this.Error("Your account is not linked to an employee record.");
            return RedirectToAction("Index", "Home");
        }

        return RedirectToAction(nameof(Details), new { id = _user.EmployeeId });
    }

    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        var model = new EmployeeFormViewModel { HireDate = DateTime.Today };
        await PopulateFormAsync(model, null, ct);
        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create(EmployeeFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            await PopulateFormAsync(model, null, ct, keepDeductions: true);
            return View(model);
        }

        var result = await _employees.CreateAsync(model.ToEntity(), DeductionMap(model), ct);
        if (!result.Succeeded)
        {
            ModelState.AddErrors(result);
            await PopulateFormAsync(model, null, ct, keepDeductions: true);
            return View(model);
        }

        this.Success($"Employee {result.Value!.FullName} ({result.Value.EmployeeNumber}) was created.");
        return RedirectToAction(nameof(Details), new { id = result.Value.Id });
    }

    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var employee = await _employees.GetAsync(id, includeDetails: true, ct);
        if (employee is null) return NotFound();

        var model = EmployeeFormViewModel.FromEntity(employee);
        await PopulateFormAsync(model, employee, ct);
        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Edit(int id, EmployeeFormViewModel model, CancellationToken ct)
    {
        if (model.Id != id) return BadRequest();

        if (!ModelState.IsValid)
        {
            await PopulateFormAsync(model, null, ct, keepDeductions: true);
            return View(model);
        }

        var result = await _employees.UpdateAsync(model.ToEntity(), DeductionMap(model), ct);
        if (!result.Succeeded)
        {
            ModelState.AddErrors(result);
            await PopulateFormAsync(model, null, ct, keepDeductions: true);
            return View(model);
        }

        this.Success("Employee details were saved.");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        var result = await _employees.DeactivateAsync(id, ct);
        if (result.Succeeded) this.Success("Employee was deactivated.");
        else this.Error(result.ErrorMessage);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Activate(int id, CancellationToken ct)
    {
        var result = await _employees.ActivateAsync(id, ct);
        if (result.Succeeded) this.Success("Employee was re-activated.");
        else this.Error(result.ErrorMessage);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> AddRaise([Bind(Prefix = "Raise")] RaiseViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            this.Error(string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            return RedirectToAction(nameof(Details), new { id = model.EmployeeId });
        }

        var result = await _employees.AddRaiseAsync(model.EmployeeId, model.NewAmount!.Value, model.EffectiveDate!.Value, model.Reason ?? string.Empty, ct);
        if (result.Succeeded) this.Success("Salary change recorded.");
        else this.Error(result.ErrorMessage);

        return RedirectToAction(nameof(Details), new { id = model.EmployeeId });
    }

    [AcceptVerbs("GET", "POST")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> CheckEmail(string email, int? id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(email)) return Json(true);
        var available = await _employees.IsEmailAvailableAsync(email, id, ct);
        return Json(available ? true : "This email is already used by another employee.");
    }

    private async Task<IEnumerable<SelectListItem>> DepartmentOptionsAsync(int? selected, CancellationToken ct)
    {
        var departments = await _departments.GetAllAsync(ct);
        return departments.Select(d => new SelectListItem(d.Name, d.Id.ToString(), d.Id == selected));
    }

    private async Task PopulateFormAsync(EmployeeFormViewModel model, Domain.Entities.Employee? employee, CancellationToken ct, bool keepDeductions = false)
    {
        model.DepartmentOptions = await DepartmentOptionsAsync(model.DepartmentId, ct);

        var types = await _settings.GetDeductionTypesAsync(ct);
        var posted = model.Deductions.ToDictionary(d => d.DeductionTypeId, d => d.Amount);
        model.Deductions = types.Select(t => new DeductionInput
        {
            DeductionTypeId = t.Id,
            Name = t.Name,
            Amount = keepDeductions
                ? posted.GetValueOrDefault(t.Id)
                : employee?.Deductions.FirstOrDefault(d => d.DeductionTypeId == t.Id)?.Amount ?? 0m
        }).ToList();
    }

    private static Dictionary<int, decimal> DeductionMap(EmployeeFormViewModel model) =>
        model.Deductions.GroupBy(d => d.DeductionTypeId).ToDictionary(g => g.Key, g => g.Last().Amount);
}
