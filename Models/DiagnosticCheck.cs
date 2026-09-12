namespace VP.Field.Diagnostic.Models;

public enum CheckState
{
    Pass,
    Warning,
    Fail,
    Info,
    NotAvailable
}

public sealed record DiagnosticCheck(
    string Section,
    string Name,
    CheckState State,
    string Value,
    string Details = "");
