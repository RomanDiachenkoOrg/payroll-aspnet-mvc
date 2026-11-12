using Payroll.Domain.Enums;

namespace Payroll.Domain.Entities;

public class Timesheet
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    /// <summary>Monday of the reported week.</summary>
    public DateTime WeekStart { get; set; }
    public TimesheetStatus Status { get; set; } = TimesheetStatus.Draft;
    public string? Notes { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TimesheetEntry> Entries { get; set; } = new List<TimesheetEntry>();

    public DateTime WeekEnd => WeekStart.AddDays(6);
    public decimal TotalHours => Entries.Sum(e => e.Hours);

    public bool IsEditable => Status is TimesheetStatus.Draft or TimesheetStatus.Rejected;
}

public class TimesheetEntry
{
    public int Id { get; set; }
    public int TimesheetId { get; set; }
    public Timesheet? Timesheet { get; set; }
    public DateTime WorkDate { get; set; }
    public decimal Hours { get; set; }
}
