using Payroll.Domain.Enums;

namespace Payroll.Domain.Entities;

public class PayRun
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public DateTime PayDate { get; set; }
    public PayRunStatus Status { get; set; } = PayRunStatus.Draft;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime? CalculatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? PaidAt { get; set; }

    public ICollection<Payslip> Payslips { get; set; } = new List<Payslip>();

    public bool IsLocked => Status == PayRunStatus.Paid;
    public decimal TotalGross => Payslips.Sum(p => p.Gross);
    public decimal TotalNet => Payslips.Sum(p => p.Net);
    public decimal TotalDeductions => Payslips.Sum(p => p.TotalDeductions);
}

public class Payslip
{
    public int Id { get; set; }
    public int PayRunId { get; set; }
    public PayRun? PayRun { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public PayType PayType { get; set; }
    public decimal? AnnualSalary { get; set; }
    public decimal? HourlyRate { get; set; }
    public decimal RegularHours { get; set; }
    public decimal OvertimeHours { get; set; }

    public decimal Gross { get; set; }
    public decimal IncomeTax { get; set; }
    public decimal SocialContribution { get; set; }
    public decimal Pension { get; set; }
    public decimal OtherDeductions { get; set; }
    public decimal Net { get; set; }

    public decimal TotalDeductions => IncomeTax + SocialContribution + Pension + OtherDeductions;
}
