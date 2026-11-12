using Microsoft.EntityFrameworkCore;
using Payroll.Domain.Abstractions;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Infrastructure.Data;

namespace Payroll.Infrastructure.Repositories;

public class TimesheetRepository : ITimesheetRepository
{
    private readonly PayrollDbContext _db;

    public TimesheetRepository(PayrollDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<Timesheet>> SearchAsync(TimesheetQuery query, CancellationToken ct = default)
    {
        IQueryable<Timesheet> q = _db.Timesheets
            .AsNoTracking()
            .Include(t => t.Employee).ThenInclude(e => e!.Department)
            .Include(t => t.Entries);

        if (query.Status.HasValue) q = q.Where(t => t.Status == query.Status.Value);
        if (query.EmployeeId.HasValue) q = q.Where(t => t.EmployeeId == query.EmployeeId.Value);
        if (query.DepartmentId.HasValue) q = q.Where(t => t.Employee!.DepartmentId == query.DepartmentId.Value);
        if (query.WeekFrom.HasValue) q = q.Where(t => t.WeekStart >= query.WeekFrom.Value);

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(t => t.WeekStart)
            .ThenBy(t => t.Employee!.LastName)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .AsSplitQuery()
            .ToListAsync(ct);

        return new PagedResult<Timesheet>(items, total, query.Page, query.PageSize);
    }

    public Task<Timesheet?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.Timesheets
            .Include(t => t.Employee).ThenInclude(e => e!.Department)
            .Include(t => t.Entries)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<bool> ExistsForWeekAsync(int employeeId, DateTime weekStart, int? excludeId = null, CancellationToken ct = default) =>
        _db.Timesheets.AnyAsync(t => t.EmployeeId == employeeId && t.WeekStart == weekStart && (excludeId == null || t.Id != excludeId), ct);

    public async Task<IReadOnlyList<Timesheet>> GetApprovedForPeriodAsync(DateTime periodStart, DateTime periodEnd, CancellationToken ct = default) =>
        await _db.Timesheets
            .AsNoTracking()
            .Include(t => t.Entries)
            .Where(t => t.Status == TimesheetStatus.Approved && t.WeekStart >= periodStart && t.WeekStart <= periodEnd)
            .ToListAsync(ct);

    public Task<int> CountByStatusAsync(TimesheetStatus status, int? departmentId = null, CancellationToken ct = default)
    {
        var q = _db.Timesheets.Where(t => t.Status == status);
        if (departmentId.HasValue) q = q.Where(t => t.Employee!.DepartmentId == departmentId.Value);
        return q.CountAsync(ct);
    }

    public void Add(Timesheet timesheet) => _db.Timesheets.Add(timesheet);

    public void RemoveEntries(IEnumerable<TimesheetEntry> entries) => _db.TimesheetEntries.RemoveRange(entries);
}
