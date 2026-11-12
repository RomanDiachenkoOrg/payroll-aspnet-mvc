using Payroll.Domain.Abstractions;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;

namespace Payroll.Domain.Services;

public sealed record DashboardSummary(
    int Headcount,
    PayRun? LastPaidRun,
    int PendingTimesheets,
    DateTime NextPayDate,
    PayRun? NextRun,
    IReadOnlyList<DepartmentCostRow> LastRunByDepartment,
    IReadOnlyList<AuditLog> RecentActivity);

public interface IDashboardService
{
    Task<DashboardSummary> GetSummaryAsync(CancellationToken ct = default);
}

public sealed class DashboardService : IDashboardService
{
    private readonly IEmployeeRepository _employees;
    private readonly ITimesheetRepository _timesheets;
    private readonly IPayRunRepository _payRuns;
    private readonly ISettingsRepository _settings;
    private readonly IAuditLogRepository _audit;
    private readonly IReportService _reports;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;

    public DashboardService(
        IEmployeeRepository employees,
        ITimesheetRepository timesheets,
        IPayRunRepository payRuns,
        ISettingsRepository settings,
        IAuditLogRepository audit,
        IReportService reports,
        ICurrentUser user,
        IClock clock)
    {
        _employees = employees;
        _timesheets = timesheets;
        _payRuns = payRuns;
        _settings = settings;
        _audit = audit;
        _reports = reports;
        _user = user;
        _clock = clock;
    }

    public async Task<DashboardSummary> GetSummaryAsync(CancellationToken ct = default)
    {
        var headcount = await _employees.CountActiveAsync(ct);
        var lastPaid = await _payRuns.GetLatestWithStatusAsync(PayRunStatus.Paid, ct);

        int? departmentFilter = _user.Role == UserRole.Manager ? _user.DepartmentId : null;
        var pending = await _timesheets.CountByStatusAsync(TimesheetStatus.Submitted, departmentFilter, ct);

        var nextRun = await _payRuns.GetNextUnpaidAsync(ct);
        DateTime nextPayDate;
        if (nextRun is not null)
        {
            nextPayDate = nextRun.PayDate;
        }
        else
        {
            var settings = await _settings.GetSettingsAsync(ct);
            var today = _clock.Today;
            nextPayDate = DateHelper.PayDateFor(DateHelper.StartOfMonth(today), settings.PayDayOfMonth);
            if (nextPayDate < today)
                nextPayDate = DateHelper.PayDateFor(DateHelper.StartOfMonth(today).AddMonths(1), settings.PayDayOfMonth);
        }

        IReadOnlyList<DepartmentCostRow> byDepartment = Array.Empty<DepartmentCostRow>();
        if (lastPaid is not null)
        {
            var report = await _reports.GetCostByDepartmentAsync(lastPaid.PeriodStart, lastPaid.PeriodEnd, ct);
            byDepartment = report.Rows;
        }

        var recent = _user.Role == UserRole.Admin
            ? await _audit.GetRecentAsync(8, ct)
            : Array.Empty<AuditLog>();

        return new DashboardSummary(headcount, lastPaid, pending, nextPayDate, nextRun, byDepartment, recent);
    }
}
