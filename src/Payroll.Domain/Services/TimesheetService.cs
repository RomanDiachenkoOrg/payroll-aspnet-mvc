using Payroll.Domain.Abstractions;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;

namespace Payroll.Domain.Services;

public sealed record TimesheetInput(int? Id, int EmployeeId, DateTime WeekStart, IReadOnlyList<decimal> DailyHours, string? Notes);

public interface ITimesheetService
{
    Task<PagedResult<Timesheet>> SearchAsync(TimesheetQuery query, CancellationToken ct = default);
    Task<Timesheet?> GetAsync(int id, CancellationToken ct = default);
    bool CanView(Timesheet timesheet);
    bool CanEdit(Timesheet timesheet);
    bool CanReview(Timesheet timesheet);
    Task<OperationResult<Timesheet>> SaveAsync(TimesheetInput input, CancellationToken ct = default);
    Task<OperationResult> SubmitAsync(int id, CancellationToken ct = default);
    Task<OperationResult> ApproveAsync(int id, CancellationToken ct = default);
    Task<OperationResult> RejectAsync(int id, string reason, CancellationToken ct = default);
}

public sealed class TimesheetService : ITimesheetService
{
    public const decimal MaxHoursPerDay = 24m;

    private readonly ITimesheetRepository _timesheets;
    private readonly IEmployeeRepository _employees;
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;

    public TimesheetService(
        ITimesheetRepository timesheets,
        IEmployeeRepository employees,
        IUnitOfWork uow,
        IAuditService audit,
        ICurrentUser user,
        IClock clock)
    {
        _timesheets = timesheets;
        _employees = employees;
        _uow = uow;
        _audit = audit;
        _user = user;
        _clock = clock;
    }

    public Task<PagedResult<Timesheet>> SearchAsync(TimesheetQuery query, CancellationToken ct = default)
    {
        // Scope the list to what the current user is allowed to see.
        switch (_user.Role)
        {
            case UserRole.Employee:
                query.EmployeeId = _user.EmployeeId ?? -1;
                query.DepartmentId = null;
                break;
            case UserRole.Manager:
                query.DepartmentId = _user.DepartmentId ?? -1;
                break;
        }

        return _timesheets.SearchAsync(query, ct);
    }

    public Task<Timesheet?> GetAsync(int id, CancellationToken ct = default) => _timesheets.GetByIdAsync(id, ct);

    public bool CanView(Timesheet timesheet) => _user.Role switch
    {
        UserRole.Admin => true,
        UserRole.Manager => timesheet.Employee?.DepartmentId == _user.DepartmentId || timesheet.EmployeeId == _user.EmployeeId,
        _ => timesheet.EmployeeId == _user.EmployeeId
    };

    public bool CanEdit(Timesheet timesheet) =>
        timesheet.IsEditable && (_user.Role == UserRole.Admin || timesheet.EmployeeId == _user.EmployeeId);

    public bool CanReview(Timesheet timesheet) =>
        timesheet.Status == TimesheetStatus.Submitted && _user.Role switch
        {
            UserRole.Admin => true,
            UserRole.Manager => timesheet.Employee?.DepartmentId == _user.DepartmentId
                                && timesheet.EmployeeId != _user.EmployeeId,
            _ => false
        };

    public async Task<OperationResult<Timesheet>> SaveAsync(TimesheetInput input, CancellationToken ct = default)
    {
        if (input.DailyHours.Count != 7)
            return OperationResult<Timesheet>.Failure("A weekly timesheet must have exactly 7 days.");

        for (var i = 0; i < 7; i++)
        {
            var h = input.DailyHours[i];
            if (h < 0 || h > MaxHoursPerDay)
                return OperationResult<Timesheet>.Failure($"Hours must be between 0 and {MaxHoursPerDay}.", $"Hours[{i}]");
        }

        if (_user.Role != UserRole.Admin && input.EmployeeId != _user.EmployeeId)
            return OperationResult<Timesheet>.Failure("You can only enter your own timesheets.");

        var employee = await _employees.GetByIdAsync(input.EmployeeId, false, ct);
        if (employee is null || !employee.IsActive)
            return OperationResult<Timesheet>.Failure("Select an active employee.", nameof(TimesheetInput.EmployeeId));

        var weekStart = DateHelper.StartOfWeek(input.WeekStart);
        if (weekStart > DateHelper.StartOfWeek(_clock.Today))
            return OperationResult<Timesheet>.Failure("Timesheets cannot be entered for future weeks.", nameof(TimesheetInput.WeekStart));

        if (await _timesheets.ExistsForWeekAsync(input.EmployeeId, weekStart, input.Id, ct))
            return OperationResult<Timesheet>.Failure("A timesheet for this employee and week already exists.", nameof(TimesheetInput.WeekStart));

        Timesheet timesheet;
        if (input.Id is null)
        {
            timesheet = new Timesheet { EmployeeId = input.EmployeeId, Status = TimesheetStatus.Draft };
            _timesheets.Add(timesheet);
        }
        else
        {
            var existing = await _timesheets.GetByIdAsync(input.Id.Value, ct);
            if (existing is null) return OperationResult<Timesheet>.Failure("Timesheet not found.");
            if (!CanEdit(existing)) return OperationResult<Timesheet>.Failure("This timesheet can no longer be edited.");
            timesheet = existing;
            timesheet.EmployeeId = input.EmployeeId;
            _timesheets.RemoveEntries(timesheet.Entries.ToList());
            timesheet.Entries.Clear();
        }

        timesheet.WeekStart = weekStart;
        timesheet.Notes = input.Notes?.Trim();
        for (var i = 0; i < 7; i++)
        {
            timesheet.Entries.Add(new TimesheetEntry { WorkDate = weekStart.AddDays(i), Hours = input.DailyHours[i] });
        }

        await _uow.SaveChangesAsync(ct);
        _audit.Record(input.Id is null ? AuditActions.Create : AuditActions.Update, nameof(Timesheet), timesheet.Id,
            $"{employee.FullName}, week of {weekStart:yyyy-MM-dd}, {timesheet.TotalHours:0.##} h");
        await _uow.SaveChangesAsync(ct);
        return OperationResult<Timesheet>.Success(timesheet);
    }

    public async Task<OperationResult> SubmitAsync(int id, CancellationToken ct = default)
    {
        var timesheet = await _timesheets.GetByIdAsync(id, ct);
        if (timesheet is null) return OperationResult.Failure("Timesheet not found.");
        if (!CanEdit(timesheet)) return OperationResult.Failure("Only draft or rejected timesheets can be submitted.");
        if (timesheet.TotalHours <= 0) return OperationResult.Failure("Enter at least one hour before submitting.");

        timesheet.Status = TimesheetStatus.Submitted;
        timesheet.SubmittedAt = _clock.UtcNow;
        timesheet.RejectionReason = null;
        _audit.Record(AuditActions.Submit, nameof(Timesheet), id, $"{timesheet.TotalHours:0.##} h");
        await _uow.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> ApproveAsync(int id, CancellationToken ct = default)
    {
        var timesheet = await _timesheets.GetByIdAsync(id, ct);
        if (timesheet is null) return OperationResult.Failure("Timesheet not found.");
        if (!CanReview(timesheet)) return OperationResult.Failure("You cannot approve this timesheet.");

        timesheet.Status = TimesheetStatus.Approved;
        timesheet.ReviewedAt = _clock.UtcNow;
        timesheet.ReviewedBy = _user.Email;
        _audit.Record(AuditActions.Approve, nameof(Timesheet), id, timesheet.Employee?.FullName);
        await _uow.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> RejectAsync(int id, string reason, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return OperationResult.Failure("A reason is required when rejecting a timesheet.", "Reason");

        var timesheet = await _timesheets.GetByIdAsync(id, ct);
        if (timesheet is null) return OperationResult.Failure("Timesheet not found.");
        if (!CanReview(timesheet)) return OperationResult.Failure("You cannot reject this timesheet.");

        timesheet.Status = TimesheetStatus.Rejected;
        timesheet.RejectionReason = reason.Trim();
        timesheet.ReviewedAt = _clock.UtcNow;
        timesheet.ReviewedBy = _user.Email;
        _audit.Record(AuditActions.Reject, nameof(Timesheet), id, reason.Trim());
        await _uow.SaveChangesAsync(ct);
        return OperationResult.Success();
    }
}
