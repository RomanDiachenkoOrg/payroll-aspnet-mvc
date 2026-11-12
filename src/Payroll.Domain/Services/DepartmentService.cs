using Payroll.Domain.Abstractions;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;

namespace Payroll.Domain.Services;

public interface IDepartmentService
{
    Task<IReadOnlyList<Department>> GetAllAsync(CancellationToken ct = default);
    Task<IDictionary<int, int>> GetHeadcountsAsync(CancellationToken ct = default);
    Task<Department?> GetAsync(int id, CancellationToken ct = default);
    Task<OperationResult<Department>> CreateAsync(Department department, CancellationToken ct = default);
    Task<OperationResult> UpdateAsync(Department department, CancellationToken ct = default);
    Task<OperationResult> DeleteAsync(int id, CancellationToken ct = default);
}

public sealed class DepartmentService : IDepartmentService
{
    private readonly IDepartmentRepository _departments;
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _audit;

    public DepartmentService(IDepartmentRepository departments, IUnitOfWork uow, IAuditService audit)
    {
        _departments = departments;
        _uow = uow;
        _audit = audit;
    }

    public Task<IReadOnlyList<Department>> GetAllAsync(CancellationToken ct = default) => _departments.GetAllAsync(ct);

    public Task<IDictionary<int, int>> GetHeadcountsAsync(CancellationToken ct = default) =>
        _departments.GetActiveHeadcountsAsync(ct);

    public Task<Department?> GetAsync(int id, CancellationToken ct = default) => _departments.GetByIdAsync(id, ct);

    public async Task<OperationResult<Department>> CreateAsync(Department department, CancellationToken ct = default)
    {
        department.Name = department.Name.Trim();
        department.Code = department.Code.Trim().ToUpperInvariant();

        if (await _departments.NameExistsAsync(department.Name, null, ct))
            return OperationResult<Department>.Failure("A department with this name already exists.", nameof(Department.Name));

        _departments.Add(department);
        await _uow.SaveChangesAsync(ct);
        _audit.Record(AuditActions.Create, nameof(Department), department.Id, department.Name);
        await _uow.SaveChangesAsync(ct);
        return OperationResult<Department>.Success(department);
    }

    public async Task<OperationResult> UpdateAsync(Department department, CancellationToken ct = default)
    {
        var existing = await _departments.GetByIdAsync(department.Id, ct);
        if (existing is null) return OperationResult.Failure("Department not found.");

        var name = department.Name.Trim();
        if (await _departments.NameExistsAsync(name, department.Id, ct))
            return OperationResult.Failure("A department with this name already exists.", nameof(Department.Name));

        existing.Name = name;
        existing.Code = department.Code.Trim().ToUpperInvariant();
        existing.Description = department.Description;
        existing.CostCenter = department.CostCenter;

        _audit.Record(AuditActions.Update, nameof(Department), existing.Id, existing.Name);
        await _uow.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> DeleteAsync(int id, CancellationToken ct = default)
    {
        var existing = await _departments.GetByIdAsync(id, ct);
        if (existing is null) return OperationResult.Failure("Department not found.");

        var active = await _departments.CountActiveEmployeesAsync(id, ct);
        if (active > 0)
            return OperationResult.Failure(
                $"Department \"{existing.Name}\" cannot be deleted because it has {active} active employee(s). Move or deactivate them first.");

        if (existing.Employees.Count > 0)
            return OperationResult.Failure(
                $"Department \"{existing.Name}\" still has former employees on record and cannot be deleted.");

        _departments.Remove(existing);
        _audit.Record(AuditActions.Delete, nameof(Department), id, existing.Name);
        await _uow.SaveChangesAsync(ct);
        return OperationResult.Success();
    }
}
