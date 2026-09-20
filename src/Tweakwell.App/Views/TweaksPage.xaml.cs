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
        var scan = App.Runtime.LastScan ?? App.Runtime.Scan();

        TweakHost.Children.Add(Card(catalog.PowerPlan));
        TweakHost.Children.Add(Card(catalog.GameMode));
        TweakHost.Children.Add(Card(catalog.GameBar));
        TweakHost.Children.Add(Card(catalog.VisualEffects));
        TweakHost.Children.Add(StartupCard(catalog.StartupApps, scan));
        TweakHost.Children.Add(TempCard(catalog.TempClean));
        TweakHost.Children.Add(ExeCard(catalog.DedicatedGpu, "Game exe for dedicated GPU"));
        TweakHost.Children.Add(ExeCard(catalog.FullscreenOptimizations, "Game exe for fullscreen optimizations"));
    }

    private static Border Card(ITweak tweak, UIElement? extra = null)
    {
        var toggle = new ToggleSwitch { IsOn = tweak.IsSelected, OnContent = "On", OffContent = "Off" };
        toggle.Toggled += (_, _) => tweak.IsSelected = toggle.IsOn;

        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var title = new TextBlock { Text = tweak.Title, Style = (Style)Application.Current.Resources["SubtitleTextBlockStyle"], TextWrapping = TextWrapping.Wrap };
        Grid.SetColumn(title, 0);
        Grid.SetColumn(toggle, 1);
        header.Children.Add(title);
        header.Children.Add(toggle);

        var stack = new StackPanel { Spacing = 8 };
        stack.Children.Add(header);
        stack.Children.Add(new TextBlock
        {
            Text = tweak.Risk == TweakRisk.Caution ? "Caution" : "Low risk",
            Foreground = tweak.Risk == TweakRisk.Caution
                ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Orange)
                : new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DarkSeaGreen),
        });
        stack.Children.Add(new TextBlock { Text = tweak.Description, TextWrapping = TextWrapping.Wrap, Opacity = 0.8 });
        if (!tweak.IsReversible)
        {
            stack.Children.Add(new TextBlock { Text = "This cannot be undone.", Opacity = 0.8 });
        }

        if (tweak.RequiresAdmin && tweak.AdminReason is not null)
        {
            stack.Children.Add(new TextBlock { Text = "Needs Administrator: " + tweak.AdminReason, TextWrapping = TextWrapping.Wrap, Opacity = 0.8 });
        }

        if (extra is not null)
        {
            stack.Children.Add(extra);
        }

        return new Border
        {
            Padding = new Thickness(16),
            CornerRadius = new CornerRadius(8),
            Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
            Child = stack,
        };
    }

    private static Border StartupCard(StartupAppsTweak tweak, ScanResult scan)
    {
        var list = new StackPanel { Spacing = 4 };
        var disableable = scan.StartupApps.Where(a => a.CanDisable).ToList();
        if (disableable.Count == 0)
        {
            list.Children.Add(new TextBlock { Text = "No per-user startup apps to disable.", Opacity = 0.7 });
        }

        foreach (var app in disableable)
        {
            var box = new CheckBox
            {
                Content = $"{app.Name} ({(app.Enabled ? "enabled" : "already disabled")})",
                IsChecked = tweak.Selected.Any(s => s.Name == app.Name),
                Tag = app,
            };
            box.Checked += (_, _) => SyncStartup(tweak, list);
            box.Unchecked += (_, _) => SyncStartup(tweak, list);
            list.Children.Add(box);
        }

        return Card(tweak, list);
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

    private static Border TempCard(TempCleanTweak tweak)
    {
        var extra = new StackPanel { Spacing = 8 };
        var shader = new CheckBox
        {
            Content = "Also clear shader cache (first launches can stutter afterward)",
            IsChecked = tweak.IncludeShaderCache,
        };
        shader.Checked += (_, _) => tweak.IncludeShaderCache = true;
        shader.Unchecked += (_, _) => tweak.IncludeShaderCache = false;
        extra.Children.Add(shader);
        extra.Children.Add(new TextBlock { Text = tweak.ShaderCacheWarning, TextWrapping = TextWrapping.Wrap, Opacity = 0.75 });
        return Card(tweak, extra);
    }

    private Border ExeCard(ITweak tweak, string pickerLabel)
    {
        var pathBox = new TextBox { IsReadOnly = true, PlaceholderText = @"C:\Games\…\game.exe" };
        if (tweak is DedicatedGpuTweak gpu && !string.IsNullOrEmpty(gpu.ExecutablePath))
        {
            pathBox.Text = gpu.ExecutablePath;
        }

        if (tweak is FullscreenOptimizationsTweak fso && !string.IsNullOrEmpty(fso.ExecutablePath))
        {
            pathBox.Text = fso.ExecutablePath;
        }

        var browse = new Button { Content = "Choose .exe", Margin = new Thickness(8, 0, 0, 0) };
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

        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(pathBox, 0);
        Grid.SetColumn(browse, 1);
        row.Children.Add(pathBox);
        row.Children.Add(browse);

        var extra = new StackPanel { Spacing = 8 };
        extra.Children.Add(new TextBlock { Text = pickerLabel, Opacity = 0.7 });
        extra.Children.Add(row);
        return Card(tweak, extra);
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
            FooterNote.Text = "Startup apps is on, but you have not chosen any.";
            return;
        }

        try
        {
            var plan = App.Runtime.Engine.Preview(catalog.All);
            if (plan.Tweaks.Count == 0)
            {
                FooterNote.Text = "Nothing selected, or the selected tweaks have no changes to make.";
                return;
            }

            var dialog = new PreviewDialog(plan) { XamlRoot = XamlRoot };
            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary)
            {
                return;
            }

            var outcome = App.Runtime.Engine.Apply(plan, createRestorePoint: plan.NeedsRestorePoint);
            if (!outcome.Applied)
            {
                FooterNote.Text = string.Join(" ", outcome.Errors);
                return;
            }

            FooterNote.Text = outcome.Errors.Count == 0
                ? "Applied. History has the log and undo."
                : "Applied with errors: " + string.Join(" ", outcome.Errors);

            if (outcome.ShowTip && App.MainAppWindow is MainWindow window)
            {
                window.OfferTip();
            }
        }
        catch (Exception ex)
        {
            FooterNote.Text = ex.Message;
        }
    }
}
