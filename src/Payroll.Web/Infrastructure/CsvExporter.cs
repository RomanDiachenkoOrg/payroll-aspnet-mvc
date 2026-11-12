using System.Globalization;
using System.Text;

namespace Payroll.Web.Infrastructure;

/// <summary>Minimal RFC 4180 CSV writer used by the export endpoints.</summary>
public sealed class CsvExporter
{
    public byte[] Write(IEnumerable<string> header, IEnumerable<IEnumerable<object?>> rows)
    {
        var sb = new StringBuilder();
        AppendLine(sb, header);
        foreach (var row in rows) AppendLine(sb, row);

        var preamble = Encoding.UTF8.GetPreamble();
        var body = Encoding.UTF8.GetBytes(sb.ToString());
        return preamble.Concat(body).ToArray();
    }

    private static void AppendLine(StringBuilder sb, IEnumerable<object?> values)
    {
        var first = true;
        foreach (var value in values)
        {
            if (!first) sb.Append(',');
            sb.Append(Escape(Format(value)));
            first = false;
        }
        sb.Append("\r\n");
    }

    private static string Format(object? value) => value switch
    {
        null => string.Empty,
        decimal d => d.ToString("0.00", CultureInfo.InvariantCulture),
        DateTime dt => dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty
    };

    private static string Escape(string value)
    {
        if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0) return value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
