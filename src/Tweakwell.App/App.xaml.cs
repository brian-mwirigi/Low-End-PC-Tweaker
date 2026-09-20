using Microsoft.UI.Xaml;

namespace Tweakwell.App;

public partial class App : Application
{
    private Window? _window;

    public static Window? MainAppWindow => ((App)Current)._window;

    public static TweakwellRuntime Runtime { get; private set; } = null!;

    public App()
    {
        InitializeComponent();
        RequestedTheme = ApplicationTheme.Dark;
        UnhandledException += (_, e) =>
        {
            try
            {
                AppPaths.EnsureCreated();
                File.AppendAllText(
                    Path.Combine(AppPaths.Root, "crash.log"),
                    $"{DateTimeOffset.Now:O} {e.Message}{Environment.NewLine}{e.Exception}{Environment.NewLine}");
            }
            catch (Exception)
            {
                // last resort
            }

            e.Handled = true;
        };
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Runtime = TweakwellRuntime.CreateDefault();
        _window = new MainWindow();
        _window.Activate();
    }
}
