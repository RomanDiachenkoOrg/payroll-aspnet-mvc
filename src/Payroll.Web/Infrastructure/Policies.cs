using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Payroll.Domain.Entities;

namespace Payroll.Web.Infrastructure;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Employee = "Employee";
    public const string ManagerOrAdmin = Admin + "," + Manager;
}

public static class Policies
{
    public const string AdminOnly = "AdminOnly";
    public const string ManagerOrAdmin = "ManagerOrAdmin";
}

public static class AppClaimTypes
{
    public const string EmployeeId = "payroll:employee_id";
    public const string DepartmentId = "payroll:department_id";
}

public static class PrincipalFactory
{
    public static ClaimsPrincipal Create(AppUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        if (user.EmployeeId.HasValue) claims.Add(new Claim(AppClaimTypes.EmployeeId, user.EmployeeId.Value.ToString()));
        if (user.DepartmentId.HasValue) claims.Add(new Claim(AppClaimTypes.DepartmentId, user.DepartmentId.Value.ToString()));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }

    public static int? GetEmployeeId(this ClaimsPrincipal principal) =>
        int.TryParse(principal.FindFirstValue(AppClaimTypes.EmployeeId), out var id) ? id : null;
}
