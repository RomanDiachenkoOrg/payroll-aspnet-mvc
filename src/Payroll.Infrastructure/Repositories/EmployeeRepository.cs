using Microsoft.EntityFrameworkCore;
using Payroll.Domain.Abstractions;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Infrastructure.Data;

namespace Payroll.Infrastructure.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly PayrollDbContext _db;

    public EmployeeRepository(PayrollDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<Employee>> SearchAsync(EmployeeQuery query, CancellationToken ct = default)
    {
        IQueryable<Employee> q = _db.Employees.AsNoTracking().Include(e => e.Department);

        q = query.Status switch
        {
            EmployeeStatusFilter.Active => q.Where(e => e.IsActive),
            EmployeeStatusFilter.Inactive => q.Where(e => !e.IsActive),
            _ => q
        };

        if (query.DepartmentId.HasValue)
            q = q.Where(e => e.DepartmentId == query.DepartmentId.Value);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            q = q.Where(e =>
                EF.Functions.Like(e.FirstName, term) ||
                EF.Functions.Like(e.LastName, term) ||
                EF.Functions.Like(e.FirstName + " " + e.LastName, term) ||
                EF.Functions.Like(e.Email, term) ||
                EF.Functions.Like(e.EmployeeNumber, term) ||
                EF.Functions.Like(e.JobTitle, term));
        }

        q = (query.Sort ?? "name").ToLowerInvariant() switch
        {
            "name_desc" => q.OrderByDescending(e => e.LastName).ThenByDescending(e => e.FirstName),
            "email" => q.OrderBy(e => e.Email),
            "email_desc" => q.OrderByDescending(e => e.Email),
            "department" => q.OrderBy(e => e.Department!.Name).ThenBy(e => e.LastName),
            "department_desc" => q.OrderByDescending(e => e.Department!.Name).ThenBy(e => e.LastName),
            "hiredate" => q.OrderBy(e => e.HireDate),
            "hiredate_desc" => q.OrderByDescending(e => e.HireDate),
            "number" => q.OrderBy(e => e.EmployeeNumber),
            "number_desc" => q.OrderByDescending(e => e.EmployeeNumber),
            _ => q.OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
        };

        var total = await q.CountAsync(ct);
        var items = await q.Skip(query.Skip).Take(query.PageSize).ToListAsync(ct);
        return new PagedResult<Employee>(items, total, query.Page, query.PageSize);
    }

    public Task<Employee?> GetByIdAsync(int id, bool includeDetails = false, CancellationToken ct = default)
    {
        IQueryable<Employee> q = _db.Employees.Include(e => e.Department).Include(e => e.Deductions);
        if (includeDetails)
        {
            q = q.Include(e => e.SalaryHistory)
                .Include(e => e.Deductions).ThenInclude(d => d.DeductionType)
                .AsSplitQuery();
        }

        return q.FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    public Task<bool> EmailExistsAsync(string email, int? excludeId = null, CancellationToken ct = default)
    {
        var normalized = email.Trim().ToLower();
        return _db.Employees.AnyAsync(e => e.Email.ToLower() == normalized && (excludeId == null || e.Id != excludeId), ct);
    }

    public async Task<IReadOnlyList<Employee>> GetPayableAsync(DateTime periodStart, DateTime periodEnd, CancellationToken ct = default)
    {
        return await _db.Employees
            .Include(e => e.Deductions)
            .Where(e => e.IsActive && e.HireDate <= periodEnd)
            .OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Employee>> GetActiveLookupAsync(int? departmentId = null, CancellationToken ct = default)
    {
        var q = _db.Employees.AsNoTracking().Where(e => e.IsActive);
        if (departmentId.HasValue) q = q.Where(e => e.DepartmentId == departmentId.Value);
        return await q.OrderBy(e => e.LastName).ThenBy(e => e.FirstName).ToListAsync(ct);
    }

    public Task<int> CountActiveAsync(CancellationToken ct = default) => _db.Employees.CountAsync(e => e.IsActive, ct);

    public async Task<string> NextEmployeeNumberAsync(CancellationToken ct = default)
    {
        var numbers = await _db.Employees.Select(e => e.EmployeeNumber).ToListAsync(ct);
        var max = numbers
            .Select(n => int.TryParse(n.TrimStart('E'), out var v) ? v : 0)
            .DefaultIfEmpty(1000)
            .Max();
        return $"E{max + 1:D4}";
    }

    public void Add(Employee employee) => _db.Employees.Add(employee);

    public void AddSalaryHistory(SalaryHistory entry) => _db.SalaryHistory.Add(entry);
}
