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
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Runtime = TweakwellRuntime.CreateDefault();
        _window = new MainWindow();
        _window.Activate();
    }
}
