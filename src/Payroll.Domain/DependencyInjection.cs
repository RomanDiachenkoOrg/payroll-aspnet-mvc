using Microsoft.Extensions.DependencyInjection;
using Payroll.Domain.Abstractions;
using Payroll.Domain.Calculation;
using Payroll.Domain.Services;

namespace Payroll.Domain;

public static class DependencyInjection
{
    public static IServiceCollection AddPayrollDomain(this IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPayrollCalculator, PayrollCalculator>();

        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<ITimesheetService, TimesheetService>();
        services.AddScoped<IPayRunService, PayRunService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ISettingsService, SettingsService>();

        return services;
    }
}
