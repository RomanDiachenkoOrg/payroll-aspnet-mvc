using Payroll.Domain.Enums;

namespace Payroll.Domain.Abstractions;

/// <summary>The user on whose behalf the current operation runs.</summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    int? UserId { get; }
    string Email { get; }
    UserRole Role { get; }
    int? EmployeeId { get; }
    int? DepartmentId { get; }
}

public interface IClock
{
    DateTime UtcNow { get; }
    DateTime Today { get; }
}

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
    public DateTime Today => DateTime.Today;
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
