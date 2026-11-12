using Payroll.Domain.Abstractions;
using Payroll.Domain.Enums;

namespace Payroll.Domain.Services;

public sealed record DepartmentCostRow(
    string Department,
    int Employees,
    int Payslips,
    decimal Gross,
    decimal IncomeTax,
    decimal SocialContribution,
    decimal Pension,
    decimal OtherDeductions,
    decimal Net);

public sealed record DepartmentCostReport(DateTime From, DateTime To, IReadOnlyList<DepartmentCostRow> Rows)
{
    public decimal TotalGross => Rows.Sum(r => r.Gross);
    public decimal TotalNet => Rows.Sum(r => r.Net);
    public decimal TotalTax => Rows.Sum(r => r.IncomeTax);
    public decimal TotalSocial => Rows.Sum(r => r.SocialContribution);
    public decimal TotalPension => Rows.Sum(r => r.Pension);
    public decimal TotalOther => Rows.Sum(r => r.OtherDeductions);
    public int TotalEmployees => Rows.Sum(r => r.Employees);
    public int TotalPayslips => Rows.Sum(r => r.Payslips);
}

public interface IReportService
{
    Task<DepartmentCostReport> GetCostByDepartmentAsync(DateTime from, DateTime to, CancellationToken ct = default);
}

public sealed class ReportService : IReportService
{
    private static readonly PayRunStatus[] FinalisedStatuses = { PayRunStatus.Approved, PayRunStatus.Paid };

    private readonly IPayRunRepository _payRuns;

    public ReportService(IPayRunRepository payRuns)
    {
        _payRuns = payRuns;
    }

    public async Task<DepartmentCostReport> GetCostByDepartmentAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var payslips = await _payRuns.GetPayslipsForPeriodAsync(from.Date, to.Date, FinalisedStatuses, ct);

        var rows = payslips
            .GroupBy(p => p.Employee?.Department?.Name ?? "(none)")
            .Select(g => new DepartmentCostRow(
                g.Key,
                g.Select(p => p.EmployeeId).Distinct().Count(),
                g.Count(),
                g.Sum(p => p.Gross),
                g.Sum(p => p.IncomeTax),
                g.Sum(p => p.SocialContribution),
                g.Sum(p => p.Pension),
                g.Sum(p => p.OtherDeductions),
                g.Sum(p => p.Net)))
            .OrderBy(r => r.Department)
            .ToList();

        return new DepartmentCostReport(from.Date, to.Date, rows);
    }
}
