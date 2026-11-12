using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Payroll.Domain.Common;

namespace Payroll.Web.Infrastructure;

public static class ControllerExtensions
{
    public const string SuccessKey = "StatusSuccess";
    public const string ErrorKey = "StatusError";

    public static void Success(this Controller controller, string message) => controller.TempData[SuccessKey] = message;

    public static void Error(this Controller controller, string message) => controller.TempData[ErrorKey] = message;

    /// <summary>Copies service errors into ModelState so they show next to the matching fields.</summary>
    public static void AddErrors(this ModelStateDictionary modelState, OperationResult result, string? prefix = null)
    {
        foreach (var error in result.Errors)
        {
            var key = error.Field is null ? string.Empty : string.IsNullOrEmpty(prefix) ? error.Field : $"{prefix}.{error.Field}";
            modelState.AddModelError(key, error.Message);
        }
    }
}
