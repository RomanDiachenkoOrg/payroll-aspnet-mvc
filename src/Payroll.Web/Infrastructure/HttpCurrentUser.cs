using System.Security.Claims;
using Payroll.Domain.Abstractions;
using Payroll.Domain.Enums;

namespace Payroll.Web.Infrastructure;

public sealed class HttpCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public HttpCurrentUser(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public int? UserId => ParseInt(ClaimTypes.NameIdentifier);

    public string Email => Principal?.FindFirstValue(ClaimTypes.Email) ?? "anonymous";

    public UserRole Role =>
        Enum.TryParse<UserRole>(Principal?.FindFirstValue(ClaimTypes.Role), out var role) ? role : UserRole.Employee;

    public int? EmployeeId => ParseInt(AppClaimTypes.EmployeeId);

    public int? DepartmentId => ParseInt(AppClaimTypes.DepartmentId);

    private int? ParseInt(string claimType) =>
        int.TryParse(Principal?.FindFirstValue(claimType), out var value) ? value : null;
}
