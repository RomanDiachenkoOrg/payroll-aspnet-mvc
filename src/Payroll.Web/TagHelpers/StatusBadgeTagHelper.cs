using Microsoft.AspNetCore.Razor.TagHelpers;
using Payroll.Domain.Enums;

namespace Payroll.Web.TagHelpers;

/// <summary>
/// Renders a coloured Bootstrap badge for workflow statuses.
/// Usage: <c>&lt;status-badge status="@run.Status" /&gt;</c>
/// </summary>
[HtmlTargetElement("status-badge", TagStructure = TagStructure.WithoutEndTag)]
public class StatusBadgeTagHelper : TagHelper
{
    public object? Status { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var (text, css) = Status switch
        {
            PayRunStatus.Draft => ("Draft", "text-bg-secondary"),
            PayRunStatus.Calculated => ("Calculated", "text-bg-info"),
            PayRunStatus.Approved => ("Approved", "text-bg-primary"),
            PayRunStatus.Paid => ("Paid", "text-bg-success"),
            TimesheetStatus.Draft => ("Draft", "text-bg-secondary"),
            TimesheetStatus.Submitted => ("Submitted", "text-bg-warning"),
            TimesheetStatus.Approved => ("Approved", "text-bg-success"),
            TimesheetStatus.Rejected => ("Rejected", "text-bg-danger"),
            true => ("Active", "text-bg-success"),
            false => ("Inactive", "text-bg-secondary"),
            null => ("-", "text-bg-light"),
            _ => (Status.ToString() ?? "-", "text-bg-light")
        };

        output.TagName = "span";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("class", $"badge {css}");
        output.Attributes.SetAttribute("data-status", text.ToLowerInvariant());
        output.Content.SetContent(text);
    }
}
