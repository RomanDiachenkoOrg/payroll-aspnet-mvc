using Payroll.Domain.Enums;

namespace Payroll.Domain.Entities;

public class Employee
{
    public int Id { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public DateTime HireDate { get; set; }
    public DateTime? TerminationDate { get; set; }

    public int DepartmentId { get; set; }
    public Department? Department { get; set; }

    public PayType PayType { get; set; }
    public decimal? AnnualSalary { get; set; }
    public decimal? HourlyRate { get; set; }
    public bool PensionOptIn { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<SalaryHistory> SalaryHistory { get; set; } = new List<SalaryHistory>();
    public ICollection<EmployeeDeduction> Deductions { get; set; } = new List<EmployeeDeduction>();
    public ICollection<Timesheet> Timesheets { get; set; } = new List<Timesheet>();
    public ICollection<Payslip> Payslips { get; set; } = new List<Payslip>();

    public string FullName => $"{FirstName} {LastName}";

    public decimal MonthlyOtherDeductions => Deductions.Sum(d => d.Amount);
}
