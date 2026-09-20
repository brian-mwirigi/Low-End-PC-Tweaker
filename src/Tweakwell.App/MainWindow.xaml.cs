using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Tweakwell.App.Views;

namespace Tweakwell.App;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Title = "Tweakwell";
        SupportTip.ActionButtonClick += (_, _) => Navigate("about");
        Nav.SelectedItem = Nav.MenuItems[0];
    }

    public void OfferTip()
    {
        SupportTip.Target = AboutItem;
        SupportTip.IsOpen = true;
    }

    public void Navigate(string tag)
    {
        foreach (var item in Nav.MenuItems.OfType<NavigationViewItem>().Concat(Nav.FooterMenuItems.OfType<NavigationViewItem>()))
        {
            if (string.Equals(item.Tag as string, tag, StringComparison.OrdinalIgnoreCase))
            {
                Nav.SelectedItem = item;
                break;
            }
        }
    }

    private void Nav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem item || item.Tag is not string tag)
        {
            return;
        }

        ContentFrame.Content = tag switch
        {
            "scan" => new ScanPage(),
            "tweaks" => new TweaksPage(),
            "history" => new HistoryPage(),
            "about" => new AboutPage(),
            _ => new ScanPage(),
        };
    }
}
