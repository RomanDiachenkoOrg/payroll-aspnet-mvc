using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Payroll.Web.TagHelpers;

/// <summary>
/// Adds the "active" class to a navbar link when the current controller matches.
/// Usage: <c>&lt;a class="nav-link" asp-controller="Employees" asp-action="Index" nav-active&gt;</c>
/// </summary>
[HtmlTargetElement("a", Attributes = "nav-active")]
public class NavLinkTagHelper : TagHelper
{
    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = default!;

    [HtmlAttributeName("asp-controller")]
    public string? Controller { get; set; }

    // Runs after the built-in anchor tag helper has generated the href.
    public override int Order => 1000;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.Attributes.RemoveAll("nav-active");

        var current = ViewContext.RouteData.Values["controller"]?.ToString();
        if (Controller is null || !string.Equals(current, Controller, StringComparison.OrdinalIgnoreCase))
            return;

        var classes = output.Attributes.TryGetAttribute("class", out var attr) ? attr.Value?.ToString() : null;
        output.Attributes.SetAttribute("class", string.IsNullOrEmpty(classes) ? "active" : classes + " active");
        output.Attributes.SetAttribute("aria-current", "page");
    }
}
