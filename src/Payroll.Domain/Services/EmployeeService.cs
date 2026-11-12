using Payroll.Domain.Abstractions;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;

namespace Payroll.Domain.Services;

public interface IEmployeeService
{
    Task<PagedResult<Employee>> SearchAsync(EmployeeQuery query, CancellationToken ct = default);
    Task<Employee?> GetAsync(int id, bool includeDetails = false, CancellationToken ct = default);
    Task<IReadOnlyList<Employee>> GetActiveLookupAsync(int? departmentId = null, CancellationToken ct = default);
    Task<bool> IsEmailAvailableAsync(string email, int? excludeId, CancellationToken ct = default);
    Task<OperationResult<Employee>> CreateAsync(Employee employee, IDictionary<int, decimal> deductions, CancellationToken ct = default);
    Task<OperationResult> UpdateAsync(Employee changes, IDictionary<int, decimal> deductions, CancellationToken ct = default);
    Task<OperationResult> DeactivateAsync(int id, CancellationToken ct = default);
    Task<OperationResult> ActivateAsync(int id, CancellationToken ct = default);
    Task<OperationResult> AddRaiseAsync(int id, decimal newAmount, DateTime effectiveDate, string reason, CancellationToken ct = default);
}

public sealed class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employees;
    private readonly IDepartmentRepository _departments;
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public EmployeeService(
        IEmployeeRepository employees,
        IDepartmentRepository departments,
        IUnitOfWork uow,
        IAuditService audit,
        ICurrentUser currentUser,
        IClock clock)
    {
        _employees = employees;
        _departments = departments;
        _uow = uow;
        _audit = audit;
        _currentUser = currentUser;
        _clock = clock;
    }

    public Task<PagedResult<Employee>> SearchAsync(EmployeeQuery query, CancellationToken ct = default) =>
        _employees.SearchAsync(query, ct);

    public Task<Employee?> GetAsync(int id, bool includeDetails = false, CancellationToken ct = default) =>
        _employees.GetByIdAsync(id, includeDetails, ct);

    public Task<IReadOnlyList<Employee>> GetActiveLookupAsync(int? departmentId = null, CancellationToken ct = default) =>
        _employees.GetActiveLookupAsync(departmentId, ct);

    public async Task<bool> IsEmailAvailableAsync(string email, int? excludeId, CancellationToken ct = default) =>
        !await _employees.EmailExistsAsync(email.Trim(), excludeId, ct);

    public async Task<OperationResult<Employee>> CreateAsync(Employee employee, IDictionary<int, decimal> deductions, CancellationToken ct = default)
    {
        var errors = await ValidateAsync(employee, null, ct);
        if (errors.Count > 0) return OperationResult<Employee>.Failure(errors);

        Normalize(employee);
        employee.EmployeeNumber = await _employees.NextEmployeeNumberAsync(ct);
        employee.IsActive = true;
        ApplyDeductions(employee, deductions);

        employee.SalaryHistory.Add(new SalaryHistory
        {
            EffectiveDate = employee.HireDate,
            PayType = employee.PayType,
            AnnualSalary = employee.AnnualSalary,
            HourlyRate = employee.HourlyRate,
            Reason = "Hire",
            ChangedBy = _currentUser.Email
        });

        _employees.Add(employee);
        await _uow.SaveChangesAsync(ct);

        _audit.Record(AuditActions.Create, nameof(Employee), employee.Id, $"{employee.EmployeeNumber} {employee.FullName}");
        await _uow.SaveChangesAsync(ct);
        return OperationResult<Employee>.Success(employee);
    }

    public async Task<OperationResult> UpdateAsync(Employee changes, IDictionary<int, decimal> deductions, CancellationToken ct = default)
    {
        var employee = await _employees.GetByIdAsync(changes.Id, includeDetails: true, ct);
        if (employee is null) return OperationResult.Failure("Employee not found.");

        var errors = await ValidateAsync(changes, changes.Id, ct);
        if (errors.Count > 0) return OperationResult.Failure(errors);

        Normalize(changes);

        var payChanged = employee.PayType != changes.PayType
                         || employee.AnnualSalary != changes.AnnualSalary
                         || employee.HourlyRate != changes.HourlyRate;

        employee.FirstName = changes.FirstName;
        employee.LastName = changes.LastName;
        employee.Email = changes.Email;
        employee.Phone = changes.Phone;
        employee.JobTitle = changes.JobTitle;
        employee.HireDate = changes.HireDate;
        employee.DepartmentId = changes.DepartmentId;
        employee.PayType = changes.PayType;
        employee.AnnualSalary = changes.AnnualSalary;
        employee.HourlyRate = changes.HourlyRate;
        employee.PensionOptIn = changes.PensionOptIn;
        ApplyDeductions(employee, deductions);

        if (payChanged)
        {
            _employees.AddSalaryHistory(new SalaryHistory
            {
                EmployeeId = employee.Id,
                EffectiveDate = _clock.Today,
                PayType = employee.PayType,
                AnnualSalary = employee.AnnualSalary,
                HourlyRate = employee.HourlyRate,
                Reason = "Adjustment",
                ChangedBy = _currentUser.Email
            });
        }

        _audit.Record(AuditActions.Update, nameof(Employee), employee.Id, employee.FullName);
        await _uow.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> DeactivateAsync(int id, CancellationToken ct = default)
    {
        var employee = await _employees.GetByIdAsync(id, false, ct);
        if (employee is null) return OperationResult.Failure("Employee not found.");
        if (!employee.IsActive) return OperationResult.Failure("Employee is already inactive.");

        employee.IsActive = false;
        employee.TerminationDate = _clock.Today;
        _audit.Record(AuditActions.Deactivate, nameof(Employee), id, employee.FullName);
        await _uow.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> ActivateAsync(int id, CancellationToken ct = default)
    {
        var employee = await _employees.GetByIdAsync(id, false, ct);
        if (employee is null) return OperationResult.Failure("Employee not found.");
        if (employee.IsActive) return OperationResult.Failure("Employee is already active.");

        employee.IsActive = true;
        employee.TerminationDate = null;
        _audit.Record(AuditActions.Activate, nameof(Employee), id, employee.FullName);
        await _uow.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> AddRaiseAsync(int id, decimal newAmount, DateTime effectiveDate, string reason, CancellationToken ct = default)
    {
        var employee = await _employees.GetByIdAsync(id, false, ct);
        if (employee is null) return OperationResult.Failure("Employee not found.");
        if (!employee.IsActive) return OperationResult.Failure("Cannot change the pay of an inactive employee.");
        if (newAmount <= 0) return OperationResult.Failure("The new amount must be greater than zero.", "NewAmount");
        if (effectiveDate.Date < employee.HireDate.Date)
            return OperationResult.Failure("The effective date cannot be before the hire date.", "EffectiveDate");

        var old = employee.PayType == PayType.Salaried ? employee.AnnualSalary : employee.HourlyRate;
        if (employee.PayType == PayType.Salaried) employee.AnnualSalary = newAmount;
        else employee.HourlyRate = newAmount;

        _employees.AddSalaryHistory(new SalaryHistory
        {
            EmployeeId = employee.Id,
            EffectiveDate = effectiveDate.Date,
            PayType = employee.PayType,
            AnnualSalary = employee.AnnualSalary,
            HourlyRate = employee.HourlyRate,
            Reason = string.IsNullOrWhiteSpace(reason) ? "Raise" : reason.Trim(),
            ChangedBy = _currentUser.Email
        });

        _audit.Record(AuditActions.SalaryChange, nameof(Employee), id, $"{old:0.00} -> {newAmount:0.00}");
        await _uow.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    private async Task<List<OperationError>> ValidateAsync(Employee employee, int? excludeId, CancellationToken ct)
    {
        var errors = new List<OperationError>();

        if (await _employees.EmailExistsAsync(employee.Email.Trim(), excludeId, ct))
            errors.Add(new OperationError(nameof(Employee.Email), "This email is already used by another employee."));

        if (await _departments.GetByIdAsync(employee.DepartmentId, ct) is null)
            errors.Add(new OperationError(nameof(Employee.DepartmentId), "Select a valid department."));

        if (employee.PayType == PayType.Salaried && employee.AnnualSalary is null or <= 0)
            errors.Add(new OperationError(nameof(Employee.AnnualSalary), "Annual salary must be greater than zero."));

        if (employee.PayType == PayType.Hourly && employee.HourlyRate is null or <= 0)
            errors.Add(new OperationError(nameof(Employee.HourlyRate), "Hourly rate must be greater than zero."));

        return errors;
    }

    private static void Normalize(Employee employee)
    {
        employee.FirstName = employee.FirstName.Trim();
        employee.LastName = employee.LastName.Trim();
        employee.Email = employee.Email.Trim().ToLowerInvariant();
        employee.JobTitle = employee.JobTitle.Trim();
        employee.HireDate = employee.HireDate.Date;

        if (employee.PayType == PayType.Salaried) employee.HourlyRate = null;
        else employee.AnnualSalary = null;
    }

    private static void ApplyDeductions(Employee employee, IDictionary<int, decimal> deductions)
    {
        foreach (var (typeId, amount) in deductions)
        {
            var existing = employee.Deductions.FirstOrDefault(d => d.DeductionTypeId == typeId);
            if (amount <= 0)
            {
                if (existing is not null) employee.Deductions.Remove(existing);
                continue;
            }

            if (existing is null)
                employee.Deductions.Add(new EmployeeDeduction { DeductionTypeId = typeId, Amount = amount });
            else
                existing.Amount = amount;
        }
    }
}
