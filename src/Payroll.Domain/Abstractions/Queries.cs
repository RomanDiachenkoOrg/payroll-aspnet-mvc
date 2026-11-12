using Payroll.Domain.Common;
using Payroll.Domain.Enums;

namespace Payroll.Domain.Abstractions;

public enum EmployeeStatusFilter
{
    Active,
    Inactive,
    All
}

public sealed class EmployeeQuery : PagedQuery
{
    public string? Search { get; set; }
    public int? DepartmentId { get; set; }
    public EmployeeStatusFilter Status { get; set; } = EmployeeStatusFilter.Active;
    /// <summary>name, email, department, hiredate, title (append _desc for descending)</summary>
    public string? Sort { get; set; }
}

public sealed class TimesheetQuery : PagedQuery
{
    public TimesheetStatus? Status { get; set; }
    public int? EmployeeId { get; set; }
    public int? DepartmentId { get; set; }
    public DateTime? WeekFrom { get; set; }
}

public sealed class AuditLogQuery : PagedQuery
{
    public string? UserEmail { get; set; }
    public string? Action { get; set; }
    public string? EntityType { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }

    public AuditLogQuery()
    {
        PageSize = 25;
    }
}
