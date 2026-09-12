using System.IO;
using System.Windows;
using Microsoft.Win32;
using VP.Field.Diagnostic.Models;
using VP.Field.Diagnostic.Services;

namespace VP.Field.Diagnostic;

public partial class MainWindow : Window
{
    private readonly DiagnosticRunner _runner = new();
    private DiagnosticReport? _report;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await RunScanAsync();
    }

    private async Task RunScanAsync()
    {
        RescanButton.IsEnabled = false;
        SaveButton.IsEnabled = false;
        CopyButton.IsEnabled = false;
        VerdictTitle.Text = "SCANNING...";
        VerdictMessage.Text = "Checking Windows, connected devices, COM ports, Windows Mobile services and live RAPI access.";
        ChecksGrid.ItemsSource = null;

        try
        {
            _report = await _runner.RunAsync();
            ChecksGrid.ItemsSource = _report.Checks;
            VerdictTitle.Text = _report.VerdictTitle;
            VerdictMessage.Text = _report.VerdictMessage;
            SaveButton.IsEnabled = true;
            CopyButton.IsEnabled = true;
        }
        catch (Exception ex)
        {
            VerdictTitle.Text = "DIAGNOSTIC ERROR";
            VerdictMessage.Text = ex.Message;
        }
        finally
        {
            RescanButton.IsEnabled = true;
        }
    }

    private async void Rescan_Click(object sender, RoutedEventArgs e) => await RunScanAsync();

    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        if (_report is null) return;
        Clipboard.SetText(ReportExporter.ToText(_report));
        MessageBox.Show(this, "The diagnostic report was copied to the clipboard.", "VP Field Diagnostic",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_report is null) return;
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var dialog = new SaveFileDialog
        {
            Title = "Save diagnostic report",
            FileName = $"VP-Field-Diagnostic-{stamp}.txt",
            Filter = "Text report (*.txt)|*.txt|JSON report (*.json)|*.json",
            AddExtension = true
        };
        if (dialog.ShowDialog(this) != true) return;
        string content = Path.GetExtension(dialog.FileName).Equals(".json", StringComparison.OrdinalIgnoreCase)
            ? ReportExporter.ToJson(_report)
            : ReportExporter.ToText(_report);
        File.WriteAllText(dialog.FileName, content);
    }
}
