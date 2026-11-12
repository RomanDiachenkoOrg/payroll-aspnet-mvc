using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Payroll.Web.TagHelpers;

/// <summary>
/// Column header link that toggles ascending / descending sort and keeps the other filters.
/// Usage: <c>&lt;sort-link field="name" current="@Model.Query.Sort"&gt;Name&lt;/sort-link&gt;</c>
/// </summary>
[HtmlTargetElement("sort-link")]
public class SortLinkTagHelper : TagHelper
{
    private readonly IUrlHelperFactory _urlHelperFactory;

    public SortLinkTagHelper(IUrlHelperFactory urlHelperFactory)
    {
        _urlHelperFactory = urlHelperFactory;
    }

    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = default!;

    public string Field { get; set; } = string.Empty;
    public string? Current { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var current = (Current ?? string.Empty).ToLowerInvariant();
        var isAsc = current == Field;
        var isDesc = current == Field + "_desc";
        var next = isAsc ? Field + "_desc" : Field;

        var values = new RouteValueDictionary();
        foreach (var (key, value) in ViewContext.HttpContext.Request.Query)
        {
            if (key.Equals("sort", StringComparison.OrdinalIgnoreCase) || key.Equals("page", StringComparison.OrdinalIgnoreCase))
                continue;
            values[key] = value.ToString();
        }
        values["sort"] = next;

        var url = _urlHelperFactory.GetUrlHelper(ViewContext);
        var href = url.Action(ViewContext.RouteData.Values["action"]?.ToString(), values);

        var content = (await output.GetChildContentAsync()).GetContent();
        var arrow = isAsc ? " &#9650;" : isDesc ? " &#9660;" : string.Empty;

        output.TagName = "a";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("href", href);
        output.Attributes.SetAttribute("class", "text-reset text-decoration-none");
        if (isAsc || isDesc)
            output.Attributes.SetAttribute("aria-sort", isAsc ? "ascending" : "descending");
        output.Content.SetHtmlContent(content + arrow);
    }
}
