using Payroll.Domain.Enums;

namespace Payroll.Domain.Entities;

public class SalaryHistory
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public DateTime EffectiveDate { get; set; }
    public PayType PayType { get; set; }
    public decimal? AnnualSalary { get; set; }
    public decimal? HourlyRate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? ChangedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
