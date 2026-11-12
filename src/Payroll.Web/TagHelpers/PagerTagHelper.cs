using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Payroll.Web.TagHelpers;

/// <summary>
/// Bootstrap pagination that keeps the current query string (filters, sort) and only changes "page".
/// Usage: <c>&lt;pager page="@Model.Page" total-pages="@Model.TotalPages" /&gt;</c>
/// </summary>
[HtmlTargetElement("pager", TagStructure = TagStructure.WithoutEndTag)]
public class PagerTagHelper : TagHelper
{
    private readonly IUrlHelperFactory _urlHelperFactory;

    public PagerTagHelper(IUrlHelperFactory urlHelperFactory)
    {
        _urlHelperFactory = urlHelperFactory;
    }

    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = default!;

    public int Page { get; set; }
    public int TotalPages { get; set; }
    public int Window { get; set; } = 2;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (TotalPages <= 1)
        {
            output.SuppressOutput();
            return;
        }

        output.TagName = "nav";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("aria-label", "Pagination");

        var ul = new TagBuilder("ul");
        ul.AddCssClass("pagination pagination-sm mb-0");

        ul.InnerHtml.AppendHtml(Item("Previous", Page - 1, disabled: Page <= 1, rel: "prev"));

        var from = Math.Max(1, Page - Window);
        var to = Math.Min(TotalPages, Page + Window);
        for (var p = from; p <= to; p++)
        {
            ul.InnerHtml.AppendHtml(Item(p.ToString(), p, active: p == Page));
        }

        ul.InnerHtml.AppendHtml(Item("Next", Page + 1, disabled: Page >= TotalPages, rel: "next"));
        output.Content.SetHtmlContent(ul);
    }

    private IHtmlContent Item(string text, int page, bool disabled = false, bool active = false, string? rel = null)
    {
        var li = new TagBuilder("li");
        li.AddCssClass("page-item");
        if (disabled) li.AddCssClass("disabled");
        if (active)
        {
            li.AddCssClass("active");
            li.Attributes["aria-current"] = "page";
        }

        var a = new TagBuilder("a");
        a.AddCssClass("page-link");
        a.Attributes["href"] = disabled ? "#" : BuildUrl(page);
        if (rel is not null) a.Attributes["rel"] = rel;
        if (disabled) a.Attributes["tabindex"] = "-1";
        a.InnerHtml.Append(text);
        li.InnerHtml.AppendHtml(a);
        return li;
    }

    private string BuildUrl(int page)
    {
        var request = ViewContext.HttpContext.Request;
        var values = new RouteValueDictionary();
        foreach (var (key, value) in request.Query)
        {
            if (!string.Equals(key, "page", StringComparison.OrdinalIgnoreCase))
                values[key] = value.ToString();
        }
        values["page"] = page;

        var url = _urlHelperFactory.GetUrlHelper(ViewContext);
        return url.Action(ViewContext.RouteData.Values["action"]?.ToString(), values) ?? "#";
    }
}
