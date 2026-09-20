using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Tweakwell.App.Views;

public sealed partial class ScanPage : Page
{
    public ScanPage()
    {
        InitializeComponent();
        Loaded += (_, _) => RunScan();
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => RunScan();

    private void RunScan()
    {
        StatusText.Text = "Scanning…";
        var scan = App.Runtime.Scan();
        StatusText.Text = scan.IsLaptop ? "Laptop detected. This scan did not change anything." : "This scan did not change anything.";

        FindingsPanel.Children.Clear();
        foreach (var finding in scan.Findings)
        {
            FindingsPanel.Children.Add(new InfoBar
            {
                Title = finding.Title,
                Message = finding.Detail,
                Severity = finding.Severity == FindingSeverity.Warning ? InfoBarSeverity.Warning : InfoBarSeverity.Informational,
                IsOpen = true,
                IsClosable = false,
            });
        }

        CpuText.Text = $"{scan.CpuName} · {scan.LogicalProcessors} logical processors";
        RamText.Text = scan.MemorySummary;

        GpuPanel.Children.Clear();
        foreach (var gpu in scan.Gpus)
        {
            var kind = gpu.Kind switch
            {
                GpuKind.Integrated => "integrated",
                GpuKind.Dedicated => "dedicated",
                _ => "unclassified",
            };
            GpuPanel.Children.Add(new TextBlock { Text = $"{gpu.Name} ({kind})", TextWrapping = TextWrapping.Wrap });
        }

        if (scan.Gpus.Count == 0)
        {
            GpuPanel.Children.Add(new TextBlock { Text = "No GPU was reported by WMI." });
        }

        StoragePanel.Children.Clear();
        foreach (var disk in scan.Disks)
        {
            StoragePanel.Children.Add(new TextBlock
            {
                Text = $"{disk.Name} · {disk.Media} · {disk.Bus}",
                TextWrapping = TextWrapping.Wrap,
            });
        }

        foreach (var volume in scan.Volumes)
        {
            StoragePanel.Children.Add(new TextBlock
            {
                Text = $"{volume.Letter}  {FormatGb(volume.FreeBytes)} free of {FormatGb(volume.SizeBytes)}",
            });
        }

        PowerText.Text = string.IsNullOrEmpty(scan.PowerPlanGuid)
            ? scan.PowerPlanName
            : $"{scan.PowerPlanName} ({scan.PowerPlanGuid})";
        ProcessText.Text = $"{scan.ProcessCount} processes (raw Process.GetProcesses count, not the Settings “background apps” list).";

        StartupList.ItemsSource = scan.StartupApps.Select(a =>
        {
            var scope = a.Source switch
            {
                StartupSource.CurrentUserRun => "Current user",
                StartupSource.LocalMachineRun => "All users (read-only here)",
                StartupSource.StartupFolder => "Startup folder",
                _ => a.Source.ToString(),
            };
            var state = a.Enabled ? "enabled" : "disabled";
            return $"{a.Name} — {state} — {scope}\n{a.Command}";
        }).ToList();
    }

    private static string FormatGb(long bytes) => $"{bytes / (1024d * 1024d * 1024d):0.#} GB";
}
