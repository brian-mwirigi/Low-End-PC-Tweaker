using Microsoft.UI.Xaml.Controls;

namespace Tweakwell.App.Views;

public sealed partial class AboutPage : Page
{
    public AboutPage()
    {
        InitializeComponent();
        CoffeeLink.NavigateUri = new Uri(SupportLinks.BuyMeACoffee);
        RepoLink.NavigateUri = new Uri("https://github.com/brian-mwirigi/tweakwell");

        if (string.IsNullOrWhiteSpace(SupportLinks.MpesaNumber))
        {
            MpesaLink.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            MpesaNote.Text = "M-Pesa Till or phone is not published yet. Use Buy Me a Coffee, or ask the author.";
        }
        else
        {
            MpesaLink.NavigateUri = new Uri("https://github.com/brian-mwirigi/tweakwell#tip");
            MpesaNote.Text = $"{SupportLinks.MpesaLabel}: {SupportLinks.MpesaNumber}";
        }
    }
}
