using System.ComponentModel;
using System.Runtime.InteropServices;
using VP.Field.Diagnostic.Models;

namespace VP.Field.Diagnostic.Services;

public static class RapiService
{
    private const uint WaitObject0 = 0x00000000;
    private const uint WaitTimeout = 0x00000102;
    private const uint InvalidFileAttributes = 0xFFFFFFFF;
    private const uint FileAttributeDirectory = 0x00000010;

    [StructLayout(LayoutKind.Sequential)]
    private struct RapiInit
    {
        public int cbSize;
        public IntPtr heRapiInit;
        public int hrRapiInit;
    }

    [DllImport("rapi.dll", ExactSpelling = true)]
    private static extern int CeRapiInitEx(ref RapiInit pRapiInit);

    [DllImport("rapi.dll", ExactSpelling = true)]
    private static extern int CeRapiUninit();

    [DllImport("rapi.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern uint CeGetFileAttributes(string lpFileName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr hObject);

    public static List<DiagnosticCheck> Run(TimeSpan timeout, out bool rapiConnected, out bool trimbleDataAccessible)
    {
        var checks = new List<DiagnosticCheck>();
        rapiConnected = false;
        trimbleDataAccessible = false;

        var dllCandidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "rapi.dll"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SysWOW64", "rapi.dll")
        };
        var present = dllCandidates.Where(File.Exists).ToArray();
        checks.Add(new("RAPI", "RAPI library", present.Length > 0 ? CheckState.Pass : CheckState.Fail,
            present.Length > 0 ? string.Join("; ", present) : "rapi.dll not found"));

        if (present.Length == 0)
        {
            checks.Add(new("RAPI", "RAPI connection", CheckState.Fail, "Not tested", "The RAPI library is missing."));
            return checks;
        }

        RapiInit init = new() { cbSize = Marshal.SizeOf<RapiInit>() };
        bool initialized = false;
        try
        {
            int hr = CeRapiInitEx(ref init);
            if (hr < 0)
            {
                checks.Add(new("RAPI", "RAPI connection", CheckState.Fail, $"Initialization failed (0x{hr:X8})"));
                return checks;
            }

            initialized = true;
            uint wait = WaitForSingleObject(init.heRapiInit, (uint)timeout.TotalMilliseconds);
            if (wait == WaitTimeout)
            {
                checks.Add(new("RAPI", "RAPI connection", CheckState.Fail, "Timed out",
                    $"No RAPI connection completed within {timeout.TotalSeconds:0} seconds."));
                return checks;
            }
            if (wait != WaitObject0)
            {
                checks.Add(new("RAPI", "RAPI connection", CheckState.Fail, $"Wait failed (0x{wait:X8})",
                    new Win32Exception(Marshal.GetLastWin32Error()).Message));
                return checks;
            }
            if (init.hrRapiInit < 0)
            {
                checks.Add(new("RAPI", "RAPI connection", CheckState.Fail, $"Connection failed (0x{init.hrRapiInit:X8})"));
                return checks;
            }

            rapiConnected = true;
            checks.Add(new("RAPI", "RAPI connection", CheckState.Pass, "Connected",
                "The Windows desktop RAPI layer established a live connection to the device."));

            uint attributes = CeGetFileAttributes(@"\Trimble Data");
            trimbleDataAccessible = attributes != InvalidFileAttributes && (attributes & FileAttributeDirectory) != 0;
            checks.Add(new("RAPI", @"\Trimble Data", trimbleDataAccessible ? CheckState.Pass : CheckState.Warning,
                trimbleDataAccessible ? "Accessible" : "Not found / not accessible",
                "Read-only directory attribute check. No file was created or modified."));
        }
        catch (BadImageFormatException ex)
        {
            checks.Add(new("RAPI", "RAPI connection", CheckState.Fail, "Architecture mismatch", ex.Message));
        }
        catch (DllNotFoundException ex)
        {
            checks.Add(new("RAPI", "RAPI connection", CheckState.Fail, "rapi.dll could not be loaded", ex.Message));
        }
        catch (EntryPointNotFoundException ex)
        {
            checks.Add(new("RAPI", "RAPI connection", CheckState.Fail, "RAPI entry point missing", ex.Message));
        }
        catch (Exception ex)
        {
            checks.Add(new("RAPI", "RAPI connection", CheckState.Fail, "Unexpected error", ex.Message));
        }
        finally
        {
            if (initialized)
            {
                try { CeRapiUninit(); } catch { }
            }
            if (init.heRapiInit != IntPtr.Zero)
            {
                try { CloseHandle(init.heRapiInit); } catch { }
            }
        }

        return checks;
    }
}
