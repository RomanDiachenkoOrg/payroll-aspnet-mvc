using Microsoft.EntityFrameworkCore;
using Payroll.Domain.Abstractions;
using Payroll.Domain.Entities;
using Payroll.Infrastructure.Data;

namespace Payroll.Infrastructure.Repositories;

public class SettingsRepository : ISettingsRepository
{
    private readonly PayrollDbContext _db;

    public SettingsRepository(PayrollDbContext db)
    {
        _db = db;
    }

    public async Task<PayrollSettings> GetSettingsAsync(CancellationToken ct = default)
    {
        var settings = await _db.PayrollSettings.OrderBy(s => s.Id).FirstOrDefaultAsync(ct);
        if (settings is not null) return settings;

        settings = new PayrollSettings();
        _db.PayrollSettings.Add(settings);
        await _db.SaveChangesAsync(ct);
        return settings;
    }

    public async Task<IReadOnlyList<TaxBracket>> GetTaxBracketsAsync(CancellationToken ct = default) =>
        await _db.TaxBrackets.AsNoTracking().OrderBy(t => t.SortOrder).ToListAsync(ct);

    public async Task<IReadOnlyList<DeductionType>> GetDeductionTypesAsync(CancellationToken ct = default) =>
        await _db.DeductionTypes.AsNoTracking().Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync(ct);

    public void ReplaceTaxBrackets(IEnumerable<TaxBracket> brackets)
    {
        _db.TaxBrackets.RemoveRange(_db.TaxBrackets);
        foreach (var b in brackets)
        {
            _db.TaxBrackets.Add(new TaxBracket
            {
                LowerBound = b.LowerBound,
                UpperBound = b.UpperBound,
                Rate = b.Rate,
                SortOrder = b.SortOrder
            });
        }
    }
}

public class UserRepository : IUserRepository
{
    private readonly PayrollDbContext _db;

    public UserRepository(PayrollDbContext db)
    {
        _db = db;
    }

    public Task<AppUser?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var normalized = email.Trim().ToLower();
        return _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalized, ct);
    }

    public Task<AppUser?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.Users.Include(u => u.Department).Include(u => u.Employee).FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<IReadOnlyList<AppUser>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Users.AsNoTracking().Include(u => u.Department).OrderBy(u => u.Email).ToListAsync(ct);
}

public class AuditLogRepository : IAuditLogRepository
{
    private readonly PayrollDbContext _db;

    public AuditLogRepository(PayrollDbContext db)
    {
        _db = db;
    }

    public void Add(AuditLog entry) => _db.AuditLogs.Add(entry);

    public async Task<Domain.Common.PagedResult<AuditLog>> SearchAsync(AuditLogQuery query, CancellationToken ct = default)
    {
        var q = _db.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.UserEmail))
            q = q.Where(a => EF.Functions.Like(a.UserEmail, $"%{query.UserEmail.Trim()}%"));
        if (!string.IsNullOrWhiteSpace(query.Action))
            q = q.Where(a => a.Action == query.Action);
        if (!string.IsNullOrWhiteSpace(query.EntityType))
            q = q.Where(a => a.EntityType == query.EntityType);
        if (query.From.HasValue)
            q = q.Where(a => a.Timestamp >= query.From.Value.Date);
        if (query.To.HasValue)
        {
            var end = query.To.Value.Date.AddDays(1);
            q = q.Where(a => a.Timestamp < end);
        }

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(a => a.Timestamp).ThenByDescending(a => a.Id)
            .Skip(query.Skip).Take(query.PageSize).ToListAsync(ct);
        return new Domain.Common.PagedResult<AuditLog>(items, total, query.Page, query.PageSize);
    }

    public async Task<IReadOnlyList<string>> GetDistinctActionsAsync(CancellationToken ct = default) =>
        await _db.AuditLogs.Select(a => a.Action).Distinct().OrderBy(a => a).ToListAsync(ct);

    public async Task<IReadOnlyList<AuditLog>> GetRecentAsync(int count, CancellationToken ct = default) =>
        await _db.AuditLogs.AsNoTracking().OrderByDescending(a => a.Timestamp).ThenByDescending(a => a.Id).Take(count).ToListAsync(ct);
}
