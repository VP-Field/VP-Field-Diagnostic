using System.ServiceProcess;
using Microsoft.Win32;
using VP.Field.Diagnostic.Models;

namespace VP.Field.Diagnostic.Services;

public static class ServiceCheckService
{
    public static IEnumerable<DiagnosticCheck> GetChecks()
    {
        foreach (var serviceName in new[] { "RapiMgr", "WcesComm" })
        {
            ServiceController? sc = null;
            try
            {
                sc = new ServiceController(serviceName);
                var status = sc.Status;
                yield return new("WINDOWS MOBILE", serviceName,
                    status == ServiceControllerStatus.Running ? CheckState.Pass : CheckState.Warning,
                    status.ToString(),
                    status == ServiceControllerStatus.Running ? "Service is running." : "Service exists but is not running.");
            }
            catch
            {
                yield return new("WINDOWS MOBILE", serviceName, CheckState.Fail, "Not found",
                    "Legacy Windows Mobile service is not installed or cannot be queried.");
            }
            finally
            {
                sc?.Dispose();
            }

            var splitValue = ReadDword($@"SYSTEM\CurrentControlSet\Services\{serviceName}", "SvcHostSplitDisable");
            yield return new("WINDOWS MOBILE", $"{serviceName} SvcHostSplitDisable",
                splitValue == 1 ? CheckState.Pass : CheckState.Info,
                splitValue?.ToString() ?? "Not set",
                splitValue == 1 ? "Legacy service-host split compatibility value is enabled." : "Value is not set to 1. This is informational; the working stack may not require it.");
        }
    }

    private static int? ReadDword(string path, string name)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(path);
            if (key?.GetValue(name) is int value) return value;
        }
        catch { }
        return null;
    }
}
