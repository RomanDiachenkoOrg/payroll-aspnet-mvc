using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Payroll.Domain.Abstractions;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Web.Validation;

namespace Payroll.Web.Models;

public class EmployeeListViewModel
{
    public EmployeeQuery Query { get; set; } = new();
    public PagedResult<Employee> Result { get; set; } = PagedResult<Employee>.Empty(10);
    public IEnumerable<SelectListItem> Departments { get; set; } = Array.Empty<SelectListItem>();
    public bool CanManage { get; set; }
}

public class EmployeeFormViewModel
{
    public int? Id { get; set; }
    public string? EmployeeNumber { get; set; }

    [Required(ErrorMessage = "First name is required.")]
    [StringLength(60)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(60)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(150)]
    [Remote("CheckEmail", "Employees", AdditionalFields = nameof(Id), ErrorMessage = "This email is already used by another employee.")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Enter a valid phone number.")]
    [StringLength(30)]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Job title is required.")]
    [StringLength(100)]
    [Display(Name = "Job title")]
    public string JobTitle { get; set; } = string.Empty;

    [Required(ErrorMessage = "Select a department.")]
    [Display(Name = "Department")]
    public int? DepartmentId { get; set; }

    [Required(ErrorMessage = "Hire date is required.")]
    [DataType(DataType.Date)]
    [NotInFuture(ErrorMessage = "Hire date cannot be in the future.")]
    [Display(Name = "Hire date")]
    public DateTime? HireDate { get; set; }

    [Display(Name = "Pay type")]
    public PayType PayType { get; set; } = PayType.Salaried;

    [Display(Name = "Annual salary (USD)")]
    [Range(typeof(decimal), "0", "10000000", ErrorMessage = "Annual salary must be between 0 and 10,000,000.")]
    [RequiredPositiveIf(nameof(PayType), PayType.Salaried, ErrorMessage = "Annual salary must be greater than zero.")]
    public decimal? AnnualSalary { get; set; }

    [Display(Name = "Hourly rate (USD)")]
    [Range(typeof(decimal), "0", "1000", ErrorMessage = "Hourly rate must be between 0 and 1,000.")]
    [RequiredPositiveIf(nameof(PayType), PayType.Hourly, ErrorMessage = "Hourly rate must be greater than zero.")]
    public decimal? HourlyRate { get; set; }

    [Display(Name = "Enrolled in pension plan (3%)")]
    public bool PensionOptIn { get; set; }

    public List<DeductionInput> Deductions { get; set; } = new();

    public IEnumerable<SelectListItem> DepartmentOptions { get; set; } = Array.Empty<SelectListItem>();

    public static EmployeeFormViewModel FromEntity(Employee e) => new()
    {
        Id = e.Id,
        EmployeeNumber = e.EmployeeNumber,
        FirstName = e.FirstName,
        LastName = e.LastName,
        Email = e.Email,
        Phone = e.Phone,
        JobTitle = e.JobTitle,
        DepartmentId = e.DepartmentId,
        HireDate = e.HireDate,
        PayType = e.PayType,
        AnnualSalary = e.AnnualSalary,
        HourlyRate = e.HourlyRate,
        PensionOptIn = e.PensionOptIn
    };

    public Employee ToEntity() => new()
    {
        Id = Id ?? 0,
        FirstName = FirstName,
        LastName = LastName,
        Email = Email,
        Phone = Phone,
        JobTitle = JobTitle,
        DepartmentId = DepartmentId ?? 0,
        HireDate = HireDate ?? DateTime.Today,
        PayType = PayType,
        AnnualSalary = AnnualSalary,
        HourlyRate = HourlyRate,
        PensionOptIn = PensionOptIn
    };
}

public class DeductionInput
{
    public int DeductionTypeId { get; set; }
    public string Name { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "5000", ErrorMessage = "Deduction must be between 0 and 5,000.")]
    public decimal Amount { get; set; }
}

public class EmployeeDetailsViewModel
{
    public Employee Employee { get; set; } = default!;
    public IReadOnlyList<Payslip> Payslips { get; set; } = Array.Empty<Payslip>();
    public bool CanManage { get; set; }
    public bool IsOwnProfile { get; set; }
    public RaiseViewModel Raise { get; set; } = new();
}

public class RaiseViewModel
{
    public int EmployeeId { get; set; }

    [Required(ErrorMessage = "Enter the new amount.")]
    [Range(typeof(decimal), "0.01", "10000000", ErrorMessage = "The new amount must be greater than zero.")]
    [Display(Name = "New amount")]
    public decimal? NewAmount { get; set; }

    [Required(ErrorMessage = "Effective date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Effective date")]
    public DateTime? EffectiveDate { get; set; } = DateTime.Today;

    [StringLength(200)]
    public string? Reason { get; set; }
}
