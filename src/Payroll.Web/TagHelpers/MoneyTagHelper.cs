using System.Globalization;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Payroll.Web.TagHelpers;

/// <summary>Formats a decimal as USD. Usage: <c>&lt;money value="@x" /&gt;</c></summary>
[HtmlTargetElement("money", TagStructure = TagStructure.WithoutEndTag)]
public class MoneyTagHelper : TagHelper
{
    private static readonly CultureInfo Usd = CultureInfo.GetCultureInfo("en-US");

    public decimal? Value { get; set; }

    /// <summary>Show negative amounts (deductions) with a leading minus sign.</summary>
    public bool Negative { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "span";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("class", "money");

        if (Value is null)
        {
            output.Content.SetContent("-");
            return;
        }

        var amount = Value.Value.ToString("C2", Usd);
        output.Content.SetContent(Negative && Value.Value != 0 ? "-" + amount : amount);
    }
}
