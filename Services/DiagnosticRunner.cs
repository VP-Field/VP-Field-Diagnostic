using VP.Field.Diagnostic.Models;

namespace VP.Field.Diagnostic.Services;

public sealed class DiagnosticRunner
{
    private HashSet<string>? _previousComPorts;

    public Task<DiagnosticReport> RunAsync() => Task.Run(() =>
    {
        var report = new DiagnosticReport();
        report.Checks.AddRange(SystemInfoService.GetChecks());

        var deviceScan = DeviceDetectionService.Scan();
        report.Checks.AddRange(deviceScan.Checks);
        report.Checks.AddRange(DeviceDetectionService.BuildComDelta(_previousComPorts, deviceScan.ComPorts));
        _previousComPorts = new HashSet<string>(deviceScan.ComPorts, StringComparer.OrdinalIgnoreCase);

        report.Checks.AddRange(ServiceCheckService.GetChecks());
        report.Checks.AddRange(InstalledSoftwareService.GetChecks());
        report.Checks.AddRange(RapiService.Run(TimeSpan.FromSeconds(6), out var rapiConnected, out var trimbleDataAccessible));

        bool deviceDetected = deviceScan.Devices.Any(d =>
            d.Name.Contains("Trimble", StringComparison.OrdinalIgnoreCase) ||
            d.Name.Contains("TSC3", StringComparison.OrdinalIgnoreCase) ||
            d.Manufacturer.Contains("Trimble", StringComparison.OrdinalIgnoreCase));

        if (!deviceDetected && !rapiConnected)
        {
            report.Verdict = CompatibilityVerdict.DeviceNotDetected;
            report.VerdictTitle = "DEVICE NOT DETECTED";
            report.VerdictMessage = "Connect the TSC3 by USB and press Rescan. Compatibility with VP Field Sync cannot be determined until a live device is available.";
        }
        else if (rapiConnected && trimbleDataAccessible)
        {
            report.Verdict = CompatibilityVerdict.Ready;
            report.VerdictTitle = "READY FOR VP FIELD SYNC";
            report.VerdictMessage = "This computer passed the live read-only TSC3 connection test. VP Field Sync can use the detected Windows Mobile/RAPI connection stack for TSC3 file synchronization.";
        }
        else if (deviceDetected)
        {
            report.Verdict = CompatibilityVerdict.ActionRequired;
            report.VerdictTitle = "ACTION REQUIRED";
            report.VerdictMessage = "The TSC3 is visible to Windows, but the complete RAPI/file-system path required by VP Field Sync is not operational. Review the failed checks before installing VP Field Sync.";
        }
        else
        {
            report.Verdict = CompatibilityVerdict.UnableToDetermine;
            report.VerdictTitle = "UNABLE TO DETERMINE";
            report.VerdictMessage = "A partial legacy connection was detected, but the diagnostic could not verify the complete TSC3 path.";
        }

        return report;
    });
}
