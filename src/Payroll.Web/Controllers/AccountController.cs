using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payroll.Domain.Abstractions;
using Payroll.Domain.Services;
using Payroll.Web.Infrastructure;
using Payroll.Web.Models;

namespace Payroll.Web.Controllers;

public class AccountController : Controller
{
    private readonly IAuthService _auth;
    private readonly IAuditService _audit;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<AccountController> _logger;

    public AccountController(IAuthService auth, IAuditService audit, IUnitOfWork uow, ILogger<AccountController> logger)
    {
        _auth = auth;
        _audit = audit;
        _uow = uow;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _auth.SignInAsync(model.Email, model.Password, ct);
        if (!result.Succeeded || result.Value is null)
        {
            _logger.LogWarning("Failed login for {Email}", model.Email);
            ModelState.AddModelError(string.Empty, result.ErrorMessage);
            model.Password = string.Empty;
            return View(model);
        }

        var principal = PrincipalFactory.Create(result.Value);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            AllowRefresh = true
        });

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return LocalRedirect(model.ReturnUrl);

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        _audit.Record(AuditActions.Logout, "AppUser", User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
        await _uow.SaveChangesAsync(ct);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData[ControllerExtensions.SuccessKey] = "You have been signed out.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied(string? returnUrl = null)
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }
}
