using Microsoft.Win32;
using VP.Field.Diagnostic.Models;

namespace VP.Field.Diagnostic.Services;

public static class InstalledSoftwareService
{
    public static IEnumerable<DiagnosticCheck> GetChecks()
    {
        var found = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        {
            try
            {
                using var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
                using var uninstall = root.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
                if (uninstall is null) continue;
                foreach (var subName in uninstall.GetSubKeyNames())
                {
                    using var sub = uninstall.OpenSubKey(subName);
                    var name = sub?.GetValue("DisplayName")?.ToString();
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    if (!name.Contains("Trimble", StringComparison.OrdinalIgnoreCase) &&
                        !name.Contains("Office Synchronizer", StringComparison.OrdinalIgnoreCase)) continue;
                    var version = sub?.GetValue("DisplayVersion")?.ToString() ?? "Unknown version";
                    found[name] = version;
                }
            }
            catch { }
        }

        if (found.Count == 0)
        {
            yield return new("SOFTWARE", "Trimble software", CheckState.Info, "No matching uninstall entries found");
            yield break;
        }

        foreach (var pair in found.OrderBy(p => p.Key))
            yield return new("SOFTWARE", pair.Key, CheckState.Info, pair.Value);
    }
}
