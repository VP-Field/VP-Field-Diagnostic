using System.Management;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using VP.Field.Diagnostic.Models;

namespace VP.Field.Diagnostic.Services;

public static class SystemInfoService
{
    public static IEnumerable<DiagnosticCheck> GetChecks()
    {
        var result = new List<DiagnosticCheck>();
        string caption = "Windows";
        string version = Environment.OSVersion.Version.ToString();
        string build = Environment.OSVersion.Version.Build.ToString();

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Caption, Version, BuildNumber FROM Win32_OperatingSystem");
            foreach (ManagementObject item in searcher.Get())
            {
                caption = item["Caption"]?.ToString() ?? caption;
                version = item["Version"]?.ToString() ?? version;
                build = item["BuildNumber"]?.ToString() ?? build;
                break;
            }
        }
        catch
        {
            // Environment fallback above is sufficient for the report.
        }

        result.Add(new("SYSTEM", "Operating system", CheckState.Info, caption));
        result.Add(new("SYSTEM", "Version / build", CheckState.Info, $"{version} / {build}"));
        result.Add(new("SYSTEM", "OS architecture", CheckState.Info, RuntimeInformation.OSArchitecture.ToString()));
        result.Add(new("SYSTEM", "Diagnostic process", CheckState.Info, RuntimeInformation.ProcessArchitecture.ToString()));

        string? release = ReadRegistryString(RegistryHive.LocalMachine,
            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "DisplayVersion", RegistryView.Registry64);
        if (!string.IsNullOrWhiteSpace(release))
            result.Add(new("SYSTEM", "Windows release", CheckState.Info, release));

        return result;
    }

    private static string? ReadRegistryString(RegistryHive hive, string path, string name, RegistryView view)
    {
        try
        {
            using var root = RegistryKey.OpenBaseKey(hive, view);
            using var key = root.OpenSubKey(path);
            return key?.GetValue(name)?.ToString();
        }
        catch { return null; }
    }
}
