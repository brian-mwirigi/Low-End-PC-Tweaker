using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Tweakwell.App.Views;

public sealed partial class HistoryPage : Page
{
    public HistoryPage()
    {
        InitializeComponent();
        Loaded += (_, _) => Reload();
    }

    private void Reload()
    {
        UndoHost.Children.Clear();
        foreach (var tweak in App.Runtime.Catalog.All)
        {
            var button = new Button
            {
                Content = tweak.IsReversible ? $"Undo {tweak.Title}" : $"{tweak.Title} (not undoable)",
                IsEnabled = tweak.IsReversible,
                Tag = tweak.Id,
            };
            button.Click += UndoOne_Click;
            UndoHost.Children.Add(button);
        }

        LogList.ItemsSource = App.Runtime.Store.ReadLog()
            .Select(e => $"{e.At.ToLocalTime():yyyy-MM-dd HH:mm}  {e.Action}  {e.TweakId}\n{e.Summary}")
            .ToList();
    }

    private void UndoOne_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string id)
        {
            return;
        }

        var errors = App.Runtime.Engine.UndoTweak(id, App.Runtime.Catalog.All);
        StatusText.Text = errors.Count == 0 ? "Undid the last backup for that tweak." : string.Join(" ", errors);
        Reload();
    }

    private void RestoreAll_Click(object sender, RoutedEventArgs e)
    {
        var errors = App.Runtime.Engine.RestoreAll(App.Runtime.Catalog.All);
        StatusText.Text = errors.Count == 0 ? "Restored every reversible backup, newest first." : string.Join(" ", errors);
        Reload();
    }
}
