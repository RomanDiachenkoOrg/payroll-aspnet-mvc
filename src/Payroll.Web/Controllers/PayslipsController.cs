using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payroll.Domain.Abstractions;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Services;
using Payroll.Web.Infrastructure;
using Payroll.Web.Models;

namespace Payroll.Web.Controllers;

[Authorize]
public class PayslipsController : Controller
{
    private readonly IPayRunService _payRuns;
    private readonly ICurrentUser _user;
    private readonly CsvExporter _csv;

    public PayslipsController(IPayRunService payRuns, ICurrentUser user, CsvExporter csv)
    {
        _payRuns = payRuns;
        _user = user;
        _csv = csv;
    }

    /// <summary>Payslips of the signed-in employee.</summary>
    public async Task<IActionResult> Mine(CancellationToken ct)
    {
        if (_user.EmployeeId is null)
        {
            this.Error("Your account is not linked to an employee record.");
            return RedirectToAction("Index", "Home");
        }

        var payslips = await _payRuns.GetPayslipsForEmployeeAsync(_user.EmployeeId.Value, finalisedOnly: true, ct);
        return View(payslips);
    }

    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var payslip = await LoadAsync(id, ct);
        if (payslip is null) return NotFound();
        if (!CanView(payslip)) return Forbid();

        return View(new PayslipViewModel { Payslip = payslip, CanSeeRun = _user.Role == UserRole.Admin });
    }

    public async Task<IActionResult> Print(int id, CancellationToken ct)
    {
        var payslip = await LoadAsync(id, ct);
        if (payslip is null) return NotFound();
        if (!CanView(payslip)) return Forbid();

        return View(new PayslipViewModel { Payslip = payslip });
    }

    public async Task<IActionResult> ExportCsv(int id, CancellationToken ct)
    {
        var payslip = await LoadAsync(id, ct);
        if (payslip is null) return NotFound();
        if (!CanView(payslip)) return Forbid();

        var e = payslip.Employee!;
        var run = payslip.PayRun!;
        var rows = new List<object?[]>
        {
            new object?[] { "Employee", $"{e.FullName} ({e.EmployeeNumber})" },
            new object?[] { "Department", e.Department?.Name },
            new object?[] { "Period", $"{run.PeriodStart:yyyy-MM-dd} - {run.PeriodEnd:yyyy-MM-dd}" },
            new object?[] { "Pay date", run.PayDate },
            new object?[] { "Pay type", payslip.PayType.ToString() },
            new object?[] { "Regular hours", payslip.RegularHours },
            new object?[] { "Overtime hours", payslip.OvertimeHours },
            new object?[] { "Gross pay", payslip.Gross },
            new object?[] { "Income tax", payslip.IncomeTax },
            new object?[] { "Social contribution", payslip.SocialContribution },
            new object?[] { "Pension", payslip.Pension },
            new object?[] { "Other deductions", payslip.OtherDeductions },
            new object?[] { "Net pay", payslip.Net }
        };

        var bytes = _csv.Write(new[] { "Item", "Value" }, rows);
        return File(bytes, "text/csv", $"payslip-{e.EmployeeNumber}-{run.PeriodStart:yyyy-MM}.csv");
    }

    private Task<Payslip?> LoadAsync(int id, CancellationToken ct) => _payRuns.GetPayslipAsync(id, ct);

    private bool CanView(Payslip payslip)
    {
        if (_user.Role == UserRole.Admin) return true;

        // Employees and managers only see their own, and only once the run is approved.
        return payslip.EmployeeId == _user.EmployeeId
               && payslip.PayRun?.Status is PayRunStatus.Approved or PayRunStatus.Paid;
    }
}
