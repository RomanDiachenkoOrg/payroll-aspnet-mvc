using Payroll.Domain.Abstractions;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;

namespace Payroll.Domain.Services;

public interface IAuthService
{
    Task<OperationResult<AppUser>> SignInAsync(string email, string password, CancellationToken ct = default);
}

public sealed class AuthService : IAuthService
{
    public const string InvalidCredentialsMessage = "Invalid email or password.";

    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly IAuditLogRepository _auditLog;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;

    public AuthService(IUserRepository users, IPasswordHasher hasher, IAuditLogRepository auditLog, IUnitOfWork uow, IClock clock)
    {
        _users = users;
        _hasher = hasher;
        _auditLog = auditLog;
        _uow = uow;
        _clock = clock;
    }

    public async Task<OperationResult<AppUser>> SignInAsync(string email, string password, CancellationToken ct = default)
    {
        var normalized = (email ?? string.Empty).Trim().ToLowerInvariant();
        var user = await _users.GetByEmailAsync(normalized, ct);

        if (user is null || !_hasher.Verify(password ?? string.Empty, user.PasswordHash))
        {
            Log(normalized, "LoginFailed", user?.Id);
            await _uow.SaveChangesAsync(ct);
            return OperationResult<AppUser>.Failure(InvalidCredentialsMessage);
        }

        if (!user.IsActive)
        {
            Log(normalized, "LoginFailed", user.Id, "Account disabled");
            await _uow.SaveChangesAsync(ct);
            return OperationResult<AppUser>.Failure("Your account has been disabled. Contact an administrator.");
        }

        user.LastLoginAt = _clock.UtcNow;
        Log(normalized, "Login", user.Id);
        await _uow.SaveChangesAsync(ct);
        return OperationResult<AppUser>.Success(user);
    }

    private void Log(string email, string action, int? userId, string? details = null) =>
        _auditLog.Add(new AuditLog
        {
            Timestamp = _clock.UtcNow,
            UserEmail = string.IsNullOrEmpty(email) ? "(blank)" : email,
            Action = action,
            EntityType = nameof(AppUser),
            EntityId = userId?.ToString(),
            Details = details
        });
}
