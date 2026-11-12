using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;

namespace Payroll.Domain.Abstractions;

public interface IDepartmentRepository
{
    Task<IReadOnlyList<Department>> GetAllAsync(CancellationToken ct = default);
    Task<Department?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<bool> NameExistsAsync(string name, int? excludeId = null, CancellationToken ct = default);
    Task<int> CountActiveEmployeesAsync(int departmentId, CancellationToken ct = default);
    Task<IDictionary<int, int>> GetActiveHeadcountsAsync(CancellationToken ct = default);
    void Add(Department department);
    void Remove(Department department);
}

public interface IEmployeeRepository
{
    Task<PagedResult<Employee>> SearchAsync(EmployeeQuery query, CancellationToken ct = default);
    Task<Employee?> GetByIdAsync(int id, bool includeDetails = false, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, int? excludeId = null, CancellationToken ct = default);
    Task<IReadOnlyList<Employee>> GetPayableAsync(DateTime periodStart, DateTime periodEnd, CancellationToken ct = default);
    Task<IReadOnlyList<Employee>> GetActiveLookupAsync(int? departmentId = null, CancellationToken ct = default);
    Task<int> CountActiveAsync(CancellationToken ct = default);
    Task<string> NextEmployeeNumberAsync(CancellationToken ct = default);
    void Add(Employee employee);
    void AddSalaryHistory(SalaryHistory entry);
}

public interface ITimesheetRepository
{
    Task<PagedResult<Timesheet>> SearchAsync(TimesheetQuery query, CancellationToken ct = default);
    Task<Timesheet?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<bool> ExistsForWeekAsync(int employeeId, DateTime weekStart, int? excludeId = null, CancellationToken ct = default);
    Task<IReadOnlyList<Timesheet>> GetApprovedForPeriodAsync(DateTime periodStart, DateTime periodEnd, CancellationToken ct = default);
    Task<int> CountByStatusAsync(TimesheetStatus status, int? departmentId = null, CancellationToken ct = default);
    void Add(Timesheet timesheet);
    void RemoveEntries(IEnumerable<TimesheetEntry> entries);
}

public interface IPayRunRepository
{
    Task<IReadOnlyList<PayRun>> GetAllAsync(CancellationToken ct = default);
    Task<PayRun?> GetByIdAsync(int id, bool includePayslips = false, CancellationToken ct = default);
    Task<bool> OverlapsAsync(DateTime start, DateTime end, int? excludeId = null, CancellationToken ct = default);
    Task<PayRun?> GetLatestWithStatusAsync(PayRunStatus status, CancellationToken ct = default);
    Task<PayRun?> GetNextUnpaidAsync(CancellationToken ct = default);
    Task<Payslip?> GetPayslipAsync(int payslipId, CancellationToken ct = default);
    Task<IReadOnlyList<Payslip>> GetPayslipsForEmployeeAsync(int employeeId, bool finalisedOnly, CancellationToken ct = default);
    Task<IReadOnlyList<Payslip>> GetPayslipsForPeriodAsync(DateTime from, DateTime to, IEnumerable<PayRunStatus> statuses, CancellationToken ct = default);
    void Add(PayRun payRun);
    void Remove(PayRun payRun);
    void RemovePayslips(IEnumerable<Payslip> payslips);
}

public interface ISettingsRepository
{
    Task<PayrollSettings> GetSettingsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TaxBracket>> GetTaxBracketsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<DeductionType>> GetDeductionTypesAsync(CancellationToken ct = default);
    void ReplaceTaxBrackets(IEnumerable<TaxBracket> brackets);
}

public interface IUserRepository
{
    Task<AppUser?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<AppUser?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<AppUser>> GetAllAsync(CancellationToken ct = default);
}

public interface IAuditLogRepository
{
    void Add(AuditLog entry);
    Task<PagedResult<AuditLog>> SearchAsync(AuditLogQuery query, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetDistinctActionsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AuditLog>> GetRecentAsync(int count, CancellationToken ct = default);
}
