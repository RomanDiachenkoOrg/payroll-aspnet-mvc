using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payroll.Domain.Common;
using Payroll.Domain.Services;
using Payroll.Web.Infrastructure;
using Payroll.Web.Models;

namespace Payroll.Web.Controllers;

[Authorize(Roles = Roles.Admin)]
public class PayRunsController : Controller
{
    private readonly IPayRunService _payRuns;
    private readonly ISettingsService _settings;
    private readonly CsvExporter _csv;

    public PayRunsController(IPayRunService payRuns, ISettingsService settings, CsvExporter csv)
    {
        _payRuns = payRuns;
        _settings = settings;
        _csv = csv;
    }

    public async Task<IActionResult> Index(CancellationToken ct) => View(await _payRuns.GetAllAsync(ct));

    public async Task<IActionResult> Create(CancellationToken ct)
    {
        var settings = await _settings.GetSettingsAsync(ct);
        var runs = await _payRuns.GetAllAsync(ct);
        var nextStart = runs.Count == 0
            ? DateHelper.StartOfMonth(DateTime.Today)
            : runs.Max(r => r.PeriodEnd).AddDays(1);
        nextStart = DateHelper.StartOfMonth(nextStart);

        return View(new CreatePayRunViewModel
        {
            PeriodStart = nextStart,
            PeriodEnd = DateHelper.EndOfMonth(nextStart),
            PayDate = DateHelper.PayDateFor(nextStart, settings.PayDayOfMonth)
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreatePayRunViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _payRuns.CreateAsync(model.PeriodStart!.Value, model.PeriodEnd!.Value, model.PayDate!.Value, model.Name, ct);
        if (!result.Succeeded)
        {
            ModelState.AddErrors(result);
            return View(model);
        }

        this.Success($"Pay run \"{result.Value!.Name}\" was created. Calculate it to generate payslips.");
        return RedirectToAction(nameof(Details), new { id = result.Value.Id });
    }

    public async Task<IActionResult> Details(int id, string? search, CancellationToken ct)
    {
        var run = await _payRuns.GetAsync(id, ct);
        if (run is null) return NotFound();

        IEnumerable<Domain.Entities.Payslip> lines = run.Payslips.OrderBy(p => p.Employee?.LastName).ThenBy(p => p.Employee?.FirstName);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            lines = lines.Where(p => p.Employee is not null &&
                                     (p.Employee.FullName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                                      p.Employee.EmployeeNumber.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                                      (p.Employee.Department?.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)));
        }

        return View(new PayRunDetailsViewModel { PayRun = run, Lines = lines.ToList(), Search = search });
    }

    [HttpPost]
    public async Task<IActionResult> Calculate(int id, CancellationToken ct) =>
        Handle(await _payRuns.CalculateAsync(id, ct), id, "Payslips were calculated. Review the lines before approving.");

    [HttpPost]
    public async Task<IActionResult> Approve(int id, CancellationToken ct) =>
        Handle(await _payRuns.ApproveAsync(id, ct), id, "Pay run was approved.");

    [HttpPost]
    public async Task<IActionResult> MarkPaid(int id, CancellationToken ct) =>
        Handle(await _payRuns.MarkPaidAsync(id, ct), id, "Pay run was marked as paid and is now locked.");

    [HttpPost]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _payRuns.DeleteAsync(id, ct);
        if (!result.Succeeded)
        {
            this.Error(result.ErrorMessage);
            return RedirectToAction(nameof(Details), new { id });
        }

        this.Success("Pay run was deleted.");
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> ExportCsv(int id, CancellationToken ct)
    {
        var run = await _payRuns.GetAsync(id, ct);
        if (run is null) return NotFound();

        var header = new[]
        {
            "EmployeeNumber", "Employee", "Department", "PayType", "RegularHours", "OvertimeHours",
            "Gross", "IncomeTax", "SocialContribution", "Pension", "OtherDeductions", "Net"
        };
        var rows = run.Payslips
            .OrderBy(p => p.Employee?.LastName)
            .Select(p => new object?[]
            {
                p.Employee?.EmployeeNumber, p.Employee?.FullName, p.Employee?.Department?.Name, p.PayType.ToString(),
                p.RegularHours, p.OvertimeHours, p.Gross, p.IncomeTax, p.SocialContribution, p.Pension, p.OtherDeductions, p.Net
            });

        var bytes = _csv.Write(header, rows);
        return File(bytes, "text/csv", $"payrun-{run.PeriodStart:yyyy-MM}.csv");
    }

    private IActionResult Handle(OperationResult result, int id, string successMessage)
    {
        if (result.Succeeded) this.Success(successMessage);
        else this.Error(result.ErrorMessage);
        return RedirectToAction(nameof(Details), new { id });
    }
}
