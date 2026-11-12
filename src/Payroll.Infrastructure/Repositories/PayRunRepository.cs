using Microsoft.EntityFrameworkCore;
using Payroll.Domain.Abstractions;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Infrastructure.Data;

namespace Payroll.Infrastructure.Repositories;

public class PayRunRepository : IPayRunRepository
{
    private readonly PayrollDbContext _db;

    public PayRunRepository(PayrollDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<PayRun>> GetAllAsync(CancellationToken ct = default) =>
        await _db.PayRuns
            .AsNoTracking()
            .Include(p => p.Payslips)
            .OrderByDescending(p => p.PeriodStart)
            .ToListAsync(ct);

    public Task<PayRun?> GetByIdAsync(int id, bool includePayslips = false, CancellationToken ct = default)
    {
        IQueryable<PayRun> q = _db.PayRuns;
        if (includePayslips)
        {
            q = q.Include(p => p.Payslips)
                .ThenInclude(s => s.Employee)
                .ThenInclude(e => e!.Department);
        }

        return q.FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public Task<bool> OverlapsAsync(DateTime start, DateTime end, int? excludeId = null, CancellationToken ct = default) =>
        _db.PayRuns.AnyAsync(p => p.PeriodStart <= end && p.PeriodEnd >= start && (excludeId == null || p.Id != excludeId), ct);

    public Task<PayRun?> GetLatestWithStatusAsync(PayRunStatus status, CancellationToken ct = default) =>
        _db.PayRuns
            .AsNoTracking()
            .Include(p => p.Payslips)
            .Where(p => p.Status == status)
            .OrderByDescending(p => p.PeriodStart)
            .FirstOrDefaultAsync(ct);

    public Task<PayRun?> GetNextUnpaidAsync(CancellationToken ct = default) =>
        _db.PayRuns
            .AsNoTracking()
            .Where(p => p.Status != PayRunStatus.Paid)
            .OrderBy(p => p.PayDate)
            .FirstOrDefaultAsync(ct);

    public Task<Payslip?> GetPayslipAsync(int payslipId, CancellationToken ct = default) =>
        _db.Payslips
            .AsNoTracking()
            .Include(p => p.PayRun)
            .Include(p => p.Employee).ThenInclude(e => e!.Department)
            .FirstOrDefaultAsync(p => p.Id == payslipId, ct);

    public async Task<IReadOnlyList<Payslip>> GetPayslipsForEmployeeAsync(int employeeId, bool finalisedOnly, CancellationToken ct = default)
    {
        var q = _db.Payslips.AsNoTracking().Include(p => p.PayRun).Where(p => p.EmployeeId == employeeId);
        if (finalisedOnly)
            q = q.Where(p => p.PayRun!.Status == PayRunStatus.Approved || p.PayRun!.Status == PayRunStatus.Paid);
        return await q.OrderByDescending(p => p.PayRun!.PeriodStart).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Payslip>> GetPayslipsForPeriodAsync(DateTime from, DateTime to, IEnumerable<PayRunStatus> statuses, CancellationToken ct = default)
    {
        var statusList = statuses.ToList();
        return await _db.Payslips
            .AsNoTracking()
            .Include(p => p.PayRun)
            .Include(p => p.Employee).ThenInclude(e => e!.Department)
            .Where(p => p.PayRun!.PeriodStart >= from && p.PayRun.PeriodEnd <= to && statusList.Contains(p.PayRun.Status))
            .ToListAsync(ct);
    }

    public void Add(PayRun payRun) => _db.PayRuns.Add(payRun);

    public void Remove(PayRun payRun) => _db.PayRuns.Remove(payRun);

    public void RemovePayslips(IEnumerable<Payslip> payslips) => _db.Payslips.RemoveRange(payslips);
}
