using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Payroll.Domain.Abstractions;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;

namespace Payroll.Web.Models;

public class TimesheetListViewModel
{
    public TimesheetQuery Query { get; set; } = new();
    public PagedResult<Timesheet> Result { get; set; } = PagedResult<Timesheet>.Empty(10);
    public bool CanReview { get; set; }
    public bool ShowEmployee { get; set; }
}

public class TimesheetFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Select an employee.")]
    [Display(Name = "Employee")]
    public int? EmployeeId { get; set; }

    [Required(ErrorMessage = "Week start is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Week starting (Monday)")]
    public DateTime? WeekStart { get; set; }

    /// <summary>Hours for Monday..Sunday.</summary>
    public List<DayHours> Days { get; set; } = new();

    [StringLength(500)]
    public string? Notes { get; set; }

    public TimesheetStatus Status { get; set; } = TimesheetStatus.Draft;
    public string? RejectionReason { get; set; }
    public bool CanChooseEmployee { get; set; }
    public IEnumerable<SelectListItem> EmployeeOptions { get; set; } = Array.Empty<SelectListItem>();

    public decimal TotalHours => Days.Sum(d => d.Hours ?? 0);

    public void EnsureSevenDays(DateTime weekStart)
    {
        while (Days.Count < 7) Days.Add(new DayHours());
        if (Days.Count > 7) Days.RemoveRange(7, Days.Count - 7);
        for (var i = 0; i < 7; i++) Days[i].Date = weekStart.AddDays(i);
    }
}

public class DayHours
{
    public DateTime Date { get; set; }

    [Range(typeof(decimal), "0", "24", ErrorMessage = "Hours must be between 0 and 24.")]
    public decimal? Hours { get; set; }
}

public class TimesheetDetailsViewModel
{
    public Timesheet Timesheet { get; set; } = default!;
    public bool CanEdit { get; set; }
    public bool CanReview { get; set; }
    public RejectTimesheetViewModel Reject { get; set; } = new();
}

public class RejectTimesheetViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Please give a reason for rejecting.")]
    [StringLength(500, MinimumLength = 5, ErrorMessage = "The reason must be between 5 and 500 characters.")]
    public string Reason { get; set; } = string.Empty;
}
