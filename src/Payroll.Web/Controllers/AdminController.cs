using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payroll.Domain.Abstractions;
using Payroll.Domain.Entities;
using Payroll.Domain.Services;
using Payroll.Web.Infrastructure;
using Payroll.Web.Models;

namespace Payroll.Web.Controllers;

[Authorize(Roles = Roles.Admin)]
public class AdminController : Controller
{
    private static readonly string[] EntityTypes =
        { "AppUser", "Department", "Employee", "PayRun", "PayrollSettings", "Report", "Timesheet" };

    private readonly ISettingsService _settings;
    private readonly IUserRepository _users;
    private readonly IAuditLogRepository _auditLog;

    public AdminController(ISettingsService settings, IUserRepository users, IAuditLogRepository auditLog)
    {
        _settings = settings;
        _users = users;
        _auditLog = auditLog;
    }

    public IActionResult Index() => RedirectToAction(nameof(Settings));

    public async Task<IActionResult> Settings(CancellationToken ct)
    {
        var settings = await _settings.GetSettingsAsync(ct);
        var brackets = await _settings.GetTaxBracketsAsync(ct);

        var model = new SettingsViewModel
        {
            SocialContributionPercent = settings.SocialContributionRate * 100,
            PensionPercent = settings.PensionRate * 100,
            OvertimeMultiplier = settings.OvertimeMultiplier,
            WeeklyOvertimeThreshold = settings.WeeklyOvertimeThreshold,
            PayDayOfMonth = settings.PayDayOfMonth,
            UpdatedAt = settings.UpdatedAt,
            UpdatedBy = settings.UpdatedBy,
            Brackets = brackets.Select(b => new TaxBracketInput
            {
                LowerBound = b.LowerBound,
                UpperBound = b.UpperBound,
                RatePercent = b.Rate * 100
            }).ToList()
        };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Settings(SettingsViewModel model, string? addBracket, CancellationToken ct)
    {
        if (addBracket is not null)
        {
            ModelState.Clear();
            var last = model.Brackets.LastOrDefault();
            model.Brackets.Add(new TaxBracketInput { LowerBound = last?.UpperBound ?? 0, RatePercent = last?.RatePercent ?? 0 });
            return View(model);
        }

        if (!ModelState.IsValid) return View(model);

        var values = new PayrollSettings
        {
            SocialContributionRate = model.SocialContributionPercent / 100,
            PensionRate = model.PensionPercent / 100,
            OvertimeMultiplier = model.OvertimeMultiplier,
            WeeklyOvertimeThreshold = model.WeeklyOvertimeThreshold,
            PayDayOfMonth = model.PayDayOfMonth
        };
        var brackets = model.Brackets
            .Where(b => !b.Remove)
            .Select(b => new TaxBracket { LowerBound = b.LowerBound, UpperBound = b.UpperBound, Rate = b.RatePercent / 100 })
            .ToList();

        var result = await _settings.UpdateAsync(values, brackets, ct);
        if (!result.Succeeded)
        {
            ModelState.AddErrors(result);
            return View(model);
        }

        this.Success("Payroll settings were saved. They apply to the next calculation.");
        return RedirectToAction(nameof(Settings));
    }

    public async Task<IActionResult> Users(CancellationToken ct) => View(await _users.GetAllAsync(ct));

    public async Task<IActionResult> AuditLog([FromQuery] AuditLogQuery query, CancellationToken ct)
    {
        var result = await _auditLog.SearchAsync(query, ct);
        return View(new AuditLogViewModel
        {
            Query = query,
            Result = result,
            Actions = await _auditLog.GetDistinctActionsAsync(ct),
            EntityTypes = EntityTypes
        });
    }
}
