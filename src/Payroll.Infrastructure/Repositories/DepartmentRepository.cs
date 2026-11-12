using Microsoft.EntityFrameworkCore;
using Payroll.Domain.Abstractions;
using Payroll.Domain.Entities;
using Payroll.Infrastructure.Data;

namespace Payroll.Infrastructure.Repositories;

public class DepartmentRepository : IDepartmentRepository
{
    private readonly PayrollDbContext _db;

    public DepartmentRepository(PayrollDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Department>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Departments.AsNoTracking().OrderBy(d => d.Name).ToListAsync(ct);

    public Task<Department?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.Departments.Include(d => d.Employees).FirstOrDefaultAsync(d => d.Id == id, ct);

    public Task<bool> NameExistsAsync(string name, int? excludeId = null, CancellationToken ct = default)
    {
        var normalized = name.Trim().ToLower();
        return _db.Departments.AnyAsync(d => d.Name.ToLower() == normalized && (excludeId == null || d.Id != excludeId), ct);
    }

    public Task<int> CountActiveEmployeesAsync(int departmentId, CancellationToken ct = default) =>
        _db.Employees.CountAsync(e => e.DepartmentId == departmentId && e.IsActive, ct);

    public async Task<IDictionary<int, int>> GetActiveHeadcountsAsync(CancellationToken ct = default) =>
        await _db.Employees
            .Where(e => e.IsActive)
            .GroupBy(e => e.DepartmentId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

    public void Add(Department department) => _db.Departments.Add(department);

    public void Remove(Department department) => _db.Departments.Remove(department);
}
