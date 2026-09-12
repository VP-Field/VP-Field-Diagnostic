using System.Management;
using System.Text.RegularExpressions;
using VP.Field.Diagnostic.Models;

namespace VP.Field.Diagnostic.Services;

public sealed record DetectedDevice(string Name, string Manufacturer, string HardwareId, string? ComPort);

public static class DeviceDetectionService
{
    private static readonly Regex ComRegex = new(@"\((COM\d+)\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static (List<DiagnosticCheck> Checks, List<DetectedDevice> Devices, HashSet<string> ComPorts) Scan()
    {
        var checks = new List<DiagnosticCheck>();
        var devices = new List<DetectedDevice>();
        var comPorts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Name, Manufacturer, PNPDeviceID, HardwareID, Status FROM Win32_PnPEntity WHERE Present = TRUE");

            foreach (ManagementObject item in searcher.Get())
            {
                var name = item["Name"]?.ToString() ?? string.Empty;
                var manufacturer = item["Manufacturer"]?.ToString() ?? string.Empty;
                var pnp = item["PNPDeviceID"]?.ToString() ?? string.Empty;
                var hardware = Flatten(item["HardwareID"]) ?? pnp;
                var com = ExtractCom(name);
                if (com is not null) comPorts.Add(com);

                bool interesting = ContainsAny(name, "Trimble", "TSC3", "Windows Mobile", "Microsoft USB Sync") ||
                                   ContainsAny(manufacturer, "Trimble", "Windows Mobile") ||
                                   ContainsAny(pnp, "WCEUSBSH", "RNDIS");

                if (interesting)
                    devices.Add(new(name, manufacturer, hardware, com));
            }
        }
        catch (Exception ex)
        {
            checks.Add(new("DEVICE", "PnP enumeration", CheckState.Warning, "Failed", ex.Message));
        }

        bool tsc3 = devices.Any(d => ContainsAny(d.Name, "Trimble", "TSC3") || ContainsAny(d.Manufacturer, "Trimble"));
        checks.Add(new("DEVICE", "TSC3 / Trimble device detected",
            tsc3 ? CheckState.Pass : CheckState.Warning,
            tsc3 ? "Yes" : "No",
            tsc3 ? "A matching connected Windows PnP device was found." : "Connect the TSC3 by USB and press Rescan."));

        foreach (var d in devices)
        {
            checks.Add(new("DEVICE", "Detected device", CheckState.Info,
                string.IsNullOrWhiteSpace(d.Name) ? "Unnamed device" : d.Name,
                $"Manufacturer: {d.Manufacturer}; Hardware ID: {d.HardwareId}"));
        }

        if (comPorts.Count == 0)
        {
            checks.Add(new("SERIAL / COM", "COM ports", CheckState.Info, "None detected",
                "A COM port is not required for all TSC3 USB modes."));
        }
        else
        {
            foreach (var port in comPorts.OrderBy(ParseComNumber))
                checks.Add(new("SERIAL / COM", "COM port", CheckState.Info, port));
        }

        return (checks, devices, comPorts);
    }

    public static IEnumerable<DiagnosticCheck> BuildComDelta(HashSet<string>? previous, HashSet<string> current)
    {
        if (previous is null || previous.Count == 0) yield break;
        foreach (var port in current.Except(previous, StringComparer.OrdinalIgnoreCase).OrderBy(ParseComNumber))
            yield return new("SERIAL / COM", "New COM port since previous scan", CheckState.Pass, port,
                "This port appeared after the previous scan and may belong to the newly connected device.");
    }

    private static string? ExtractCom(string name)
    {
        var match = ComRegex.Match(name);
        return match.Success ? match.Groups[1].Value.ToUpperInvariant() : null;
    }

    private static bool ContainsAny(string value, params string[] needles) =>
        needles.Any(n => value.Contains(n, StringComparison.OrdinalIgnoreCase));

    private static string? Flatten(object? value)
    {
        if (value is string[] values) return string.Join(" | ", values);
        return value?.ToString();
    }

    private static int ParseComNumber(string value) =>
        int.TryParse(value.AsSpan(3), out var n) ? n : int.MaxValue;
}
