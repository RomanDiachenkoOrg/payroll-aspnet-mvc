using System.ComponentModel.DataAnnotations;
using Payroll.Domain.Abstractions;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Services;

namespace Payroll.Web.Models;

public class CreatePayRunViewModel
{
    [StringLength(100)]
    public string? Name { get; set; }

    [Required(ErrorMessage = "Period start is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Period start")]
    public DateTime? PeriodStart { get; set; }

    [Required(ErrorMessage = "Period end is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Period end")]
    public DateTime? PeriodEnd { get; set; }

    [Required(ErrorMessage = "Pay date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Pay date")]
    public DateTime? PayDate { get; set; }
}

public class PayRunDetailsViewModel
{
    public PayRun PayRun { get; set; } = default!;
    public IReadOnlyList<Payslip> Lines { get; set; } = Array.Empty<Payslip>();
    public string? Search { get; set; }
}

public class PayslipViewModel
{
    public Payslip Payslip { get; set; } = default!;
    public string CompanyName { get; set; } = "Acme Corp";
    public bool CanSeeRun { get; set; }
}

public class ReportViewModel
{
    [DataType(DataType.Date)]
    public DateTime From { get; set; }

    [DataType(DataType.Date)]
    public DateTime To { get; set; }

    public DepartmentCostReport? Report { get; set; }
}

public class DashboardViewModel
{
    public DashboardSummary Summary { get; set; } = default!;
    public string Role { get; set; } = string.Empty;
    public decimal MaxDepartmentGross => Summary.LastRunByDepartment.Count == 0 ? 0 : Summary.LastRunByDepartment.Max(r => r.Gross);
}

public class SettingsViewModel
{
    [Range(0, 100, ErrorMessage = "Enter a percentage between 0 and 100.")]
    [Display(Name = "Social contribution (%)")]
    public decimal SocialContributionPercent { get; set; }

    [Range(0, 100, ErrorMessage = "Enter a percentage between 0 and 100.")]
    [Display(Name = "Pension contribution (%)")]
    public decimal PensionPercent { get; set; }

    [Range(1, 3, ErrorMessage = "Multiplier must be between 1 and 3.")]
    [Display(Name = "Overtime multiplier")]
    public decimal OvertimeMultiplier { get; set; }

    [Range(1, 80, ErrorMessage = "Threshold must be between 1 and 80 hours.")]
    [Display(Name = "Weekly overtime threshold (hours)")]
    public decimal WeeklyOvertimeThreshold { get; set; }

    [Range(1, 31, ErrorMessage = "Pay day must be between 1 and 31.")]
    [Display(Name = "Pay day of month")]
    public int PayDayOfMonth { get; set; }

    public List<TaxBracketInput> Brackets { get; set; } = new();

    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

public class TaxBracketInput
{
    [Required]
    [Range(typeof(decimal), "0", "100000000")]
    [Display(Name = "From")]
    public decimal LowerBound { get; set; }

    [Range(typeof(decimal), "0", "100000000")]
    [Display(Name = "To")]
    public decimal? UpperBound { get; set; }

    [Range(0, 100, ErrorMessage = "Rate must be between 0 and 100%.")]
    [Display(Name = "Rate (%)")]
    public decimal RatePercent { get; set; }

    public bool Remove { get; set; }
}

public class AuditLogViewModel
{
    public AuditLogQuery Query { get; set; } = new();
    public PagedResult<AuditLog> Result { get; set; } = PagedResult<AuditLog>.Empty(25);
    public IReadOnlyList<string> Actions { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> EntityTypes { get; set; } = Array.Empty<string>();
}
