using Microsoft.UI.Xaml.Controls;

namespace Tweakwell.App.Views;

public sealed partial class PreviewDialog : ContentDialog
{
    public PreviewDialog(ApplyPlan plan)
    {
        InitializeComponent();

        if (plan.NeedsAdmin)
        {
            Body.Children.Add(new InfoBar
            {
                Title = "Administrator will be requested",
                Message = string.Join(" ", plan.AdminReasons),
                Severity = InfoBarSeverity.Warning,
                IsOpen = true,
                IsClosable = false,
            });
        }

        foreach (var tweak in plan.Tweaks)
        {
            Body.Children.Add(new TextBlock
            {
                Text = tweak.Tweak.Title + (tweak.Tweak.IsReversible ? "" : " (cannot undo)"),
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
            });

            foreach (var change in tweak.Changes)
            {
                Body.Children.Add(new TextBlock
                {
                    Text = $"{change.Target}  {change.DisplayPath}\n  was: {change.OldValue}\n  will be: {change.NewValue}",
                    TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
                    FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Cascadia Mono, Consolas"),
                    FontSize = 12,
                });
            }
        }
    }
}
