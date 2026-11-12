using Payroll.Domain.Abstractions;
using Payroll.Domain.Entities;

namespace Payroll.Domain.Services;

public interface IAuditService
{
    /// <summary>Queues an audit entry; it is persisted with the next SaveChanges.</summary>
    void Record(string action, string entityType, object? entityId, string? details = null);
}

public sealed class AuditService : IAuditService
{
    private readonly IAuditLogRepository _repository;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public AuditService(IAuditLogRepository repository, ICurrentUser currentUser, IClock clock)
    {
        _repository = repository;
        _currentUser = currentUser;
        _clock = clock;
    }

    public void Record(string action, string entityType, object? entityId, string? details = null)
    {
        _repository.Add(new AuditLog
        {
            Timestamp = _clock.UtcNow,
            UserEmail = _currentUser.IsAuthenticated ? _currentUser.Email : "system",
            Action = action,
            EntityType = entityType,
            EntityId = entityId?.ToString(),
            Details = details is { Length: > 500 } ? details[..500] : details
        });
    }
}

public static class AuditActions
{
    public const string Login = "Login";
    public const string LoginFailed = "LoginFailed";
    public const string Logout = "Logout";
    public const string Create = "Create";
    public const string Update = "Update";
    public const string Delete = "Delete";
    public const string Deactivate = "Deactivate";
    public const string Activate = "Activate";
    public const string SalaryChange = "SalaryChange";
    public const string Submit = "Submit";
    public const string Approve = "Approve";
    public const string Reject = "Reject";
    public const string Calculate = "Calculate";
    public const string MarkPaid = "MarkPaid";
    public const string Export = "Export";
    public const string SettingsChange = "SettingsChange";
}
