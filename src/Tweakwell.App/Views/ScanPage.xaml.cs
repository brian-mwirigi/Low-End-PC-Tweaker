using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Tweakwell.App.Views;

public sealed partial class ScanPage : Page
{
    public ScanPage()
    {
        InitializeComponent();
        Loaded += (_, _) => _ = RunScanAsync(force: false);
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => _ = RunScanAsync(force: true);

    private async Task RunScanAsync(bool force)
    {
        if (!force && App.Runtime.LastScan is { } cached)
        {
            Render(cached);
            return;
        }

        LeadText.Text = "Scanning…";
        RefreshButton.IsEnabled = false;
        try
        {
            Render(await App.Runtime.ScanAsync());
        }
        catch (Exception ex)
        {
            LeadText.Text = "Scan failed: " + ex.Message;
        }
        finally
        {
            RefreshButton.IsEnabled = true;
        }
    }

    private void Render(ScanResult scan)
    {
        LeadText.Text = scan.IsLaptop
            ? "Laptop. This pass was read-only — registry, power plan, and disk are untouched."
            : "This pass was read-only — registry, power plan, and disk are untouched.";

        FindingsHost.Children.Clear();
        foreach (var finding in scan.Findings)
        {
            FindingsHost.Children.Add(Theme.Finding(finding));
        }

        SpecGrid.Children.Clear();
        Place(Theme.Spec("CPU", scan.CpuName, $"{scan.LogicalProcessors} logical processors"), 0, 0);
        Place(Theme.Spec("RAM", scan.MemorySummary, "Installed memory, not available after Chrome."), 0, 1);

        var gpuValue = scan.Gpus.Count == 0
            ? "None reported"
            : string.Join("\n", scan.Gpus.Select(g => g.Name));
        var gpuHint = scan.Gpus.Count == 0
            ? "WMI did not return a video controller."
            : string.Join(" · ", scan.Gpus.Select(g => g.Kind switch
            {
                GpuKind.Integrated => "integrated",
                GpuKind.Dedicated => "dedicated",
                _ => "unclassified",
            }));
        Place(Theme.Spec("GPU", gpuValue, gpuHint), 0, 2);

        var disks = scan.Disks.Count == 0
            ? "Unknown"
            : string.Join("\n", scan.Disks.Select(d => $"{d.Name} · {Theme.Media(d.Media)}"));
        var vol = scan.Volumes.FirstOrDefault(v => v.Letter.StartsWith("C", StringComparison.OrdinalIgnoreCase))
                  ?? scan.Volumes.FirstOrDefault();
        var diskHint = vol is null
            ? ""
            : $"{vol.Letter} {FormatGb(vol.FreeBytes)} free of {FormatGb(vol.SizeBytes)}";
        Place(Theme.Spec("Storage", disks, diskHint), 1, 0);

        Place(Theme.Spec(
            "Power plan",
            string.IsNullOrWhiteSpace(scan.PowerPlanName) ? "Unknown" : scan.PowerPlanName,
            string.IsNullOrWhiteSpace(scan.PowerPlanGuid) ? null : scan.PowerPlanGuid), 1, 1);

        Place(Theme.Spec(
            "Processes",
            scan.ProcessCount.ToString(),
            "Raw process count, not Settings background apps."), 1, 2);

        var on = scan.StartupApps.Count(a => a.Enabled);
        StartupCount.Text = $"{scan.StartupApps.Count} listed · {on} on";
        StartupHost.Children.Clear();
        foreach (var app in scan.StartupApps)
        {
            StartupHost.Children.Add(Theme.StartupRow(app));
        }

        if (scan.StartupApps.Count == 0)
        {
            StartupHost.Children.Add(Theme.Body("No startup entries found in the user Run key or Startup folder.", muted: true));
        }
    }

    private void Place(Border card, int row, int column)
    {
        Grid.SetRow(card, row);
        Grid.SetColumn(card, column);
        SpecGrid.Children.Add(card);
    }

    private static string FormatGb(long bytes) => $"{bytes / (1024d * 1024d * 1024d):0.#} GB";
}
