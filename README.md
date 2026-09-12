# VP Field Diagnostic

Windows diagnostic utility for evaluating whether a PC can communicate with legacy Trimble® TSC3 field controllers and whether the computer is ready to run **VP Field Sync**.

> **Project status:** Alpha 0.1 — active development. The diagnostic is intentionally read-only.

## Alpha 0.1 checks

- Windows version, build and architecture
- Connected PnP devices matching Trimble / TSC3 / Windows Mobile
- Current serial / COM ports
- Newly appeared COM ports between scans
- `RapiMgr` and `WcesComm` services
- `SvcHostSplitDisable` values (reported, never changed)
- RAPI library presence
- Live `CeRapiInitEx` connection test with timeout
- Read-only `\Trimble Data` accessibility test
- Installed Trimble-related uninstall entries
- Final VP Field Sync compatibility verdict
- TXT and JSON report export

A green **READY FOR VP FIELD SYNC** verdict is issued only after a live RAPI connection succeeds and the diagnostic can read the TSC3 `\Trimble Data` directory. COM-port presence is informational and is not required because not every TSC3 USB mode exposes a serial port.

## Safety

Alpha 0.1 performs no writes to the controller and makes no system configuration changes.

## Build

```powershell
dotnet restore
dotnet publish -c Release -r win-x86 --self-contained true -p:PublishSingleFile=true
```

The first build targets x86 deliberately so that we can test against the legacy Windows Mobile/RAPI stack found on the known-good Windows 11 machine. Architecture support will be expanded after empirical testing.

## Language

The application, installer, reports, logs, documentation and legal text are English-only.

## Legal

This is an independent project and is not affiliated with, endorsed by, sponsored by or supported by Trimble Inc.

See [LICENSE](LICENSE), [EULA.txt](EULA.txt), [DISCLAIMER.md](DISCLAIMER.md) and [TRADEMARKS.md](TRADEMARKS.md).
