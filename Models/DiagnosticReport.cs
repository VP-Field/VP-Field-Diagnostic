using System.Text.Json.Serialization;

namespace VP.Field.Diagnostic.Models;

public enum CompatibilityVerdict
{
    Ready,
    ActionRequired,
    DeviceNotDetected,
    UnableToDetermine
}

public sealed class DiagnosticReport
{
    public string Application { get; init; } = "VP Field Diagnostic";
    public string Version { get; init; } = "Alpha 0.1";
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;
    public List<DiagnosticCheck> Checks { get; init; } = [];
    public CompatibilityVerdict Verdict { get; set; }
    public string VerdictTitle { get; set; } = string.Empty;
    public string VerdictMessage { get; set; } = string.Empty;

    [JsonIgnore]
    public bool IsReady => Verdict == CompatibilityVerdict.Ready;
}
