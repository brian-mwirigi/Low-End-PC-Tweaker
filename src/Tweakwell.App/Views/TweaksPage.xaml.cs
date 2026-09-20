using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;

namespace Tweakwell.App.Views;

public sealed partial class TweaksPage : Page
{
    public TweaksPage()
    {
        InitializeComponent();
        Loaded += (_, _) => Build();
    }

    private void Build()
    {
        TweakHost.Children.Clear();
        var catalog = App.Runtime.Catalog;
        var scan = App.Runtime.LastScan ?? new ScanResult(
            "Unknown",
            Environment.ProcessorCount,
            0,
            [],
            [],
            [],
            "Unknown",
            "",
            false,
            0,
            [],
            []);

        TweakHost.Children.Add(Row(1, catalog.PowerPlan));
        TweakHost.Children.Add(Row(2, catalog.GameMode));
        TweakHost.Children.Add(Row(3, catalog.GameBar));
        TweakHost.Children.Add(Row(4, catalog.VisualEffects));
        TweakHost.Children.Add(Row(5, catalog.StartupApps, StartupExtra(catalog.StartupApps, scan)));
        TweakHost.Children.Add(Row(6, catalog.TempClean, TempExtra(catalog.TempClean)));
        TweakHost.Children.Add(Row(7, catalog.DedicatedGpu, ExeExtra(catalog.DedicatedGpu, "Game .exe")));
        TweakHost.Children.Add(Row(8, catalog.FullscreenOptimizations, ExeExtra(catalog.FullscreenOptimizations, "Game .exe")));
        CountSelected();
    }

    private Border Row(int index, ITweak tweak, UIElement? extra = null)
    {
        var toggle = new ToggleSwitch
        {
            IsOn = tweak.IsSelected,
            OnContent = "Include",
            OffContent = "Skip",
            MinWidth = 0,
        };

        var titleRow = new Grid { ColumnSpacing = 14 };
        titleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });
        titleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        titleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var indexBlock = Theme.MonoText(index.ToString("00"), 13, muted: true);
        indexBlock.VerticalAlignment = VerticalAlignment.Top;
        indexBlock.Margin = new Thickness(0, 4, 0, 0);
        var titles = new StackPanel { Spacing = 6 };
        titles.Children.Add(Theme.Title(tweak.Title, 16));
        var chips = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        chips.Children.Add(tweak.Risk == TweakRisk.Caution
            ? Theme.Chip("Caution", Theme.Bg, Theme.Caution)
            : Theme.Chip("Low risk", Theme.Bg, Theme.Sage));
        if (!tweak.IsReversible)
        {
            chips.Children.Add(Theme.Chip("Cannot undo", Theme.Muted, Theme.SurfaceRaised));
        }

        if (tweak.RequiresAdmin)
        {
            chips.Children.Add(Theme.Chip("Admin", Theme.Amber, Theme.SurfaceRaised));
        }

        titles.Children.Add(chips);
        Grid.SetColumn(indexBlock, 0);
        Grid.SetColumn(titles, 1);
        Grid.SetColumn(toggle, 2);
        titleRow.Children.Add(indexBlock);
        titleRow.Children.Add(titles);
        titleRow.Children.Add(toggle);

        var stack = new StackPanel { Spacing = 10 };
        stack.Children.Add(titleRow);
        stack.Children.Add(Theme.Body(tweak.Description, muted: true));
        if (tweak.RequiresAdmin && tweak.AdminReason is not null)
        {
            stack.Children.Add(Theme.Body("Administrator: " + tweak.AdminReason, muted: true));
        }

        if (extra is not null)
        {
            stack.Children.Add(extra);
        }

        var card = Theme.Card(stack, new Thickness(16, 16, 18, 16));
        void Paint()
        {
            tweak.IsSelected = toggle.IsOn;
            card.BorderThickness = tweak.IsSelected ? new Thickness(2, 1, 1, 1) : new Thickness(1);
            card.BorderBrush = Theme.Brush(tweak.IsSelected ? Theme.Amber : Theme.Line);
            CountSelected();
        }

        toggle.Toggled += (_, _) => Paint();
        Paint();
        return card;
    }

    private void CountSelected()
    {
        if (SelectedCount is null)
        {
            return;
        }

        var n = App.Runtime.Catalog.All.Count(t => t.IsSelected);
        SelectedCount.Text = n == 0 ? "0 included" : $"{n} included";
    }

    private static UIElement StartupExtra(StartupAppsTweak tweak, ScanResult scan)
    {
        var list = new StackPanel { Spacing = 2 };
        var disableable = scan.StartupApps.Where(a => a.CanDisable).ToList();
        if (disableable.Count == 0)
        {
            list.Children.Add(Theme.Body("No per-user startup apps to disable.", muted: true));
            return list;
        }

        foreach (var app in disableable)
        {
            var box = new CheckBox
            {
                Content = $"{app.Name}  ·  {(app.Enabled ? "currently on" : "already off")}",
                IsChecked = tweak.Selected.Any(s => s.Name == app.Name),
                Tag = app,
                Foreground = Theme.Brush(Theme.Text),
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe UI"),
            };
            box.Checked += (_, _) => SyncStartup(tweak, list);
            box.Unchecked += (_, _) => SyncStartup(tweak, list);
            list.Children.Add(box);
        }

        return list;
    }

    private static void SyncStartup(StartupAppsTweak tweak, StackPanel list)
    {
        tweak.Selected.Clear();
        foreach (var box in list.Children.OfType<CheckBox>().Where(c => c.IsChecked == true && c.Tag is StartupApp))
        {
            var app = (StartupApp)box.Tag;
            tweak.Selected.Add(new StartupPick(app.Name, app.Source));
        }
    }

    private static UIElement TempExtra(TempCleanTweak tweak)
    {
        var extra = new StackPanel { Spacing = 8 };
        var shader = new CheckBox
        {
            Content = "Also clear shader cache (first launches can stutter)",
            IsChecked = tweak.IncludeShaderCache,
            Foreground = Theme.Brush(Theme.Text),
        };
        shader.Checked += (_, _) => tweak.IncludeShaderCache = true;
        shader.Unchecked += (_, _) => tweak.IncludeShaderCache = false;
        extra.Children.Add(shader);
        extra.Children.Add(Theme.Body(tweak.ShaderCacheWarning, muted: true));
        return extra;
    }

    private UIElement ExeExtra(ITweak tweak, string label)
    {
        var pathBox = new TextBox
        {
            IsReadOnly = true,
            PlaceholderText = @"C:\Games\game.exe",
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe UI"),
        };
        if (tweak is DedicatedGpuTweak gpu && !string.IsNullOrEmpty(gpu.ExecutablePath))
        {
            pathBox.Text = gpu.ExecutablePath;
        }

        if (tweak is FullscreenOptimizationsTweak fso && !string.IsNullOrEmpty(fso.ExecutablePath))
        {
            pathBox.Text = fso.ExecutablePath;
        }

        var browse = new Button { Content = "Choose .exe", Style = (Style)Application.Current.Resources["GhostButton"] };
        browse.Click += async (_, _) =>
        {
            var picked = await PickExeAsync();
            if (picked is null)
            {
                return;
            }

            if (!AntiCheat.IsSafeGameExecutable(picked, out var reason))
            {
                FooterNote.Text = reason;
                return;
            }

            pathBox.Text = picked;
            if (tweak is DedicatedGpuTweak g)
            {
                g.ExecutablePath = picked;
            }

            if (tweak is FullscreenOptimizationsTweak f)
            {
                f.ExecutablePath = picked;
            }
        };

        var row = new Grid { ColumnSpacing = 8 };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(pathBox, 0);
        Grid.SetColumn(browse, 1);
        row.Children.Add(pathBox);
        row.Children.Add(browse);

        var extra = new StackPanel { Spacing = 8 };
        extra.Children.Add(Theme.Label(label.ToUpperInvariant()));
        extra.Children.Add(row);
        extra.Children.Add(Theme.Body("Registry only. The game folder is not touched.", muted: true));
        return extra;
    }

    private async Task<string?> PickExeAsync()
    {
        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(".exe");
        picker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainAppWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
        var file = await picker.PickSingleFileAsync();
        return file?.Path;
    }

    private async void Preview_Click(object sender, RoutedEventArgs e)
    {
        FooterNote.Text = "";
        var catalog = App.Runtime.Catalog;
        if (catalog.StartupApps.IsSelected && catalog.StartupApps.Selected.Count == 0)
        {
            FooterNote.Text = "Startup is included, but no apps are checked.";
            return;
        }

        SetBusy(true, "Building preview…");
        try
        {
            var plan = await App.Runtime.Engine.PreviewAsync(catalog.All);
            if (plan.Tweaks.Count == 0)
            {
                FooterNote.Text = "Nothing selected, or the selected rows have no changes.";
                return;
            }

            SetBusy(false, "");
            var dialog = new PreviewDialog(plan) { XamlRoot = XamlRoot };
            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary)
            {
                return;
            }

            var applyNote = plan.NeedsRestorePoint
                ? "Applying… Windows may ask for administrator. A restore point can take up to a minute."
                : "Applying selected tweaks…";
            SetBusy(true, applyNote);

            var outcome = await App.Runtime.Engine.ApplyAsync(plan, createRestorePoint: plan.NeedsRestorePoint);
            if (!outcome.Applied)
            {
                FooterNote.Text = string.Join(" ", outcome.Errors);
                return;
            }

            var restore = outcome.Backup?.RestorePointMessage;
            var applied = outcome.Errors.Count == 0
                ? "Applied. History has the log and undo."
                : "Applied with errors: " + string.Join(" ", outcome.Errors);
            FooterNote.Text = string.IsNullOrWhiteSpace(restore) ? applied : applied + " " + restore;

            if (outcome.ShowTip && App.MainAppWindow is MainWindow window)
            {
                window.OfferTip();
            }
        }
        catch (Exception ex)
        {
            FooterNote.Text = ex.Message;
        }
        finally
        {
            SetBusy(false, FooterNote.Text);
        }
    }

    private void SetBusy(bool busy, string message)
    {
        PreviewButton.IsEnabled = !busy;
        BusyRing.IsActive = busy;
        BusyRing.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
        if (!string.IsNullOrEmpty(message) || !busy)
        {
            FooterNote.Text = message;
        }
    }
}
