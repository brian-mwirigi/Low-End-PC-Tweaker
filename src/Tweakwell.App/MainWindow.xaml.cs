using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Tweakwell.App.Views;
using Windows.Graphics;

namespace Tweakwell.App;

public sealed partial class MainWindow : Window
{
    private string _page = "scan";

    public MainWindow()
    {
        InitializeComponent();
        Title = "Tweakwell";
        try
        {
            AppWindow?.Resize(new SizeInt32(1180, 800));
        }
        catch (Exception)
        {
            // default size is fine
        }

        Show("scan");
    }

    public void OfferTip()
    {
        Show("about");
    }

    public void Navigate(string tag) => Show(tag);

    private void Nav_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string tag)
        {
            Show(tag);
        }
    }

    private void Show(string tag)
    {
        _page = tag;
        PaintNav();
        ShellNote.Text = tag switch
        {
            "scan" => "Scan changes nothing",
            "tweaks" => "Nothing writes until you confirm",
            "history" => "Undo from the last backup",
            "about" => "No telemetry · no ads",
            _ => "",
        };

        ContentFrame.Content = tag switch
        {
            "tweaks" => new TweaksPage(),
            "history" => new HistoryPage(),
            "about" => new AboutPage(),
            _ => new ScanPage(),
        };
    }

    private void PaintNav()
    {
        Apply(NavScan, "scan");
        Apply(NavTweaks, "tweaks");
        Apply(NavHistory, "history");
        Apply(NavAbout, "about");
    }

    private void Apply(Button button, string tag)
    {
        button.Style = (Style)Application.Current.Resources[_page == tag ? "NavPillActive" : "NavPill"];
    }
}
