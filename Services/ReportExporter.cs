using System.Text;
using System.Text.Json;
using VP.Field.Diagnostic.Models;

namespace VP.Field.Diagnostic.Services;

public static class ReportExporter
{
    public static string ToText(DiagnosticReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{report.Application} {report.Version}");
        sb.AppendLine($"Generated: {report.Timestamp:O}");
        sb.AppendLine();
        foreach (var group in report.Checks.GroupBy(c => c.Section))
        {
            sb.AppendLine($"[{group.Key}]");
            foreach (var c in group)
            {
                sb.AppendLine($"{c.State,-12} {c.Name}: {c.Value}");
                if (!string.IsNullOrWhiteSpace(c.Details)) sb.AppendLine($"             {c.Details}");
            }
            sb.AppendLine();
        }
        sb.AppendLine("[COMPATIBILITY RESULT]");
        sb.AppendLine(report.VerdictTitle);
        sb.AppendLine(report.VerdictMessage);
        return sb.ToString();
    }

    public static string ToJson(DiagnosticReport report) => JsonSerializer.Serialize(report, new JsonSerializerOptions
    {
        WriteIndented = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    });
}
