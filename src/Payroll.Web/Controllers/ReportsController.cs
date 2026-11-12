using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payroll.Domain.Abstractions;
using Payroll.Domain.Common;
using Payroll.Domain.Services;
using Payroll.Web.Infrastructure;
using Payroll.Web.Models;

namespace Payroll.Web.Controllers;

[Authorize(Roles = Roles.ManagerOrAdmin)]
public class ReportsController : Controller
{
    private readonly IReportService _reports;
    private readonly IAuditService _audit;
    private readonly IUnitOfWork _uow;
    private readonly CsvExporter _csv;

    public ReportsController(IReportService reports, IAuditService audit, IUnitOfWork uow, CsvExporter csv)
    {
        _reports = reports;
        _audit = audit;
        _uow = uow;
        _csv = csv;
    }

    public async Task<IActionResult> Index(DateTime? from, DateTime? to, CancellationToken ct)
    {
        var (start, end) = Normalize(from, to);
        if (end < start)
        {
            ModelState.AddModelError("To", "The end date must be after the start date.");
            return View(new ReportViewModel { From = start, To = end });
        }

        var report = await _reports.GetCostByDepartmentAsync(start, end, ct);
        return View(new ReportViewModel { From = start, To = end, Report = report });
    }

    public async Task<IActionResult> ExportCsv(DateTime? from, DateTime? to, CancellationToken ct)
    {
        var (start, end) = Normalize(from, to);
        var report = await _reports.GetCostByDepartmentAsync(start, end, ct);

        var header = new[] { "Department", "Employees", "Payslips", "Gross", "IncomeTax", "SocialContribution", "Pension", "OtherDeductions", "Net" };
        var rows = report.Rows
            .Select(r => new object?[] { r.Department, r.Employees, r.Payslips, r.Gross, r.IncomeTax, r.SocialContribution, r.Pension, r.OtherDeductions, r.Net })
            .Append(new object?[] { "Total", report.TotalEmployees, report.TotalPayslips, report.TotalGross, report.TotalTax, report.TotalSocial, report.TotalPension, report.TotalOther, report.TotalNet });

        _audit.Record(AuditActions.Export, "Report", null, $"Cost by department {start:yyyy-MM-dd}..{end:yyyy-MM-dd}");
        await _uow.SaveChangesAsync(ct);

        return File(_csv.Write(header, rows), "text/csv", $"payroll-cost-{start:yyyyMMdd}-{end:yyyyMMdd}.csv");
    }

    private static (DateTime From, DateTime To) Normalize(DateTime? from, DateTime? to)
    {
        // Default: the last three full months plus the current one.
        var start = from?.Date ?? DateHelper.StartOfMonth(DateTime.Today).AddMonths(-3);
        var end = to?.Date ?? DateHelper.EndOfMonth(DateTime.Today);
        return (start, end);
    }
}
