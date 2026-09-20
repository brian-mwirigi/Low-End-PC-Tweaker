using Microsoft.UI.Xaml.Controls;

namespace Tweakwell.App.Views;

public sealed partial class PreviewDialog : ContentDialog
{
    public PreviewDialog(ApplyPlan plan)
    {
        InitializeComponent();

        if (plan.NeedsAdmin)
        {
            Body.Children.Add(Theme.Finding(new Finding(
                "Administrator will be requested",
                string.Join(" ", plan.AdminReasons),
                FindingSeverity.Warning)));
        }

        foreach (var tweak in plan.Tweaks)
        {
            var block = new StackPanel { Spacing = 6 };
            block.Children.Add(Theme.Title(tweak.Tweak.Title + (tweak.Tweak.IsReversible ? "" : "  ·  cannot undo"), 15));
            foreach (var change in tweak.Changes)
            {
                block.Children.Add(Theme.Label(change.Target.ToUpperInvariant()));
                block.Children.Add(Theme.Body(change.DisplayPath));
                block.Children.Add(Theme.Body($"was  {change.OldValue}", muted: true));
                block.Children.Add(Theme.Body($"will be  {change.NewValue}"));
            }

            Body.Children.Add(Theme.Card(block));
        }
    }
}
