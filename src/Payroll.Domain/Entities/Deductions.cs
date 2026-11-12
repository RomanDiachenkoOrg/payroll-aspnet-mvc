namespace Payroll.Domain.Entities;

/// <summary>
/// A voluntary or contractual deduction taken from pay every month
/// (health insurance, union dues, garnishments...).
/// </summary>
public class DeductionType
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal DefaultAmount { get; set; }
    public bool IsActive { get; set; } = true;
}

public class EmployeeDeduction
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public int DeductionTypeId { get; set; }
    public DeductionType? DeductionType { get; set; }
    public decimal Amount { get; set; }
}
