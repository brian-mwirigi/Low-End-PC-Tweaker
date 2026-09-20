using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Tweakwell.App.Views;

public sealed partial class PreviewDialog : ContentDialog
{
    public PreviewDialog(ApplyPlan plan)
    {
        InitializeComponent();

        Body.Children.Add(Theme.Body("Nothing has been written yet. This is the old value next to the new one.", muted: true));

        if (plan.NeedsAdmin)
        {
            Body.Children.Add(Theme.Finding(new Finding(
                "Administrator will be requested",
                string.Join(" ", plan.AdminReasons),
                FindingSeverity.Warning)));
        }

        var n = 1;
        foreach (var tweak in plan.Tweaks)
        {
            var block = new StackPanel { Spacing = 4 };
            var head = new Grid { ColumnSpacing = 12 };
            head.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });
            head.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var index = Theme.MonoText(n.ToString("00"), 13, muted: true);
            index.VerticalAlignment = VerticalAlignment.Center;
            var title = Theme.Title(tweak.Tweak.Title + (tweak.Tweak.IsReversible ? "" : "  ·  cannot undo"), 16);
            Grid.SetColumn(index, 0);
            Grid.SetColumn(title, 1);
            head.Children.Add(index);
            head.Children.Add(title);
            block.Children.Add(head);
            foreach (var change in tweak.Changes)
            {
                block.Children.Add(Theme.ProofChange(change));
            }

            Body.Children.Add(Theme.Card(block, new Thickness(16, 14, 16, 14)));
            n++;
        }
    }
}
