using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payroll.Domain.Abstractions;
using Payroll.Domain.Services;
using Payroll.Web.Models;

namespace Payroll.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly IDashboardService _dashboard;
    private readonly ICurrentUser _user;

    public HomeController(IDashboardService dashboard, ICurrentUser user)
    {
        _dashboard = dashboard;
        _user = user;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var summary = await _dashboard.GetSummaryAsync(ct);
        return View(new DashboardViewModel { Summary = summary, Role = _user.Role.ToString() });
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [AllowAnonymous]
    [Route("/Home/StatusCode")]
    public IActionResult StatusCodePage(int code)
    {
        Response.StatusCode = code;
        return View("StatusCode", new ErrorViewModel { StatusCode = code });
    }
}
