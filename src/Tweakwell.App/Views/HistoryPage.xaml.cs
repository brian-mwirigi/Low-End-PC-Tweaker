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
                Content = tweak.IsReversible ? $"Undo  {tweak.Title}" : $"{tweak.Title}  ·  cannot undo",
                IsEnabled = tweak.IsReversible,
                Tag = tweak.Id,
                Style = (Style)Application.Current.Resources["GhostButton"],
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
            };
            button.Click += UndoOne_Click;
            UndoHost.Children.Add(button);
        }

        LogHost.Children.Clear();
        var entries = App.Runtime.Store.ReadLog();
        if (entries.Count == 0)
        {
            LogHost.Children.Add(Theme.Body("Nothing has been applied yet.", muted: true));
            return;
        }

        foreach (var entry in entries)
        {
            var row = new StackPanel { Spacing = 2, Margin = new Thickness(0, 8, 0, 8) };
            row.Children.Add(Theme.Label($"{entry.At.ToLocalTime():yyyy-MM-dd HH:mm}  ·  {entry.Action}  ·  {entry.TweakId}"));
            row.Children.Add(Theme.Body(entry.Summary));
            LogHost.Children.Add(row);
        }
    }

    private async void UndoOne_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string id)
        {
            return;
        }

        SetBusy(true, "Undoing… Windows may ask for administrator.");
        try
        {
            var errors = await App.Runtime.Engine.UndoTweakAsync(id, App.Runtime.Catalog.All);
            StatusText.Text = errors.Count == 0 ? "Undid the last backup for that tweak." : string.Join(" ", errors);
            Reload();
        }
        catch (Exception ex)
        {
            StatusText.Text = ex.Message;
        }
        finally
        {
            SetBusy(false, StatusText.Text);
        }
    }

    private async void RestoreAll_Click(object sender, RoutedEventArgs e)
    {
        SetBusy(true, "Restoring every reversible backup…");
        try
        {
            var errors = await App.Runtime.Engine.RestoreAllAsync(App.Runtime.Catalog.All);
            StatusText.Text = errors.Count == 0 ? "Restored every reversible backup, newest first." : string.Join(" ", errors);
            Reload();
        }
        catch (Exception ex)
        {
            StatusText.Text = ex.Message;
        }
        finally
        {
            SetBusy(false, StatusText.Text);
        }
    }

    private void SetBusy(bool busy, string message)
    {
        RestoreAllButton.IsEnabled = !busy;
        foreach (var button in UndoHost.Children.OfType<Button>())
        {
            if (button.Tag is string)
            {
                button.IsEnabled = !busy && button.Content is string text && !text.Contains("cannot undo", StringComparison.Ordinal);
            }
        }

        StatusText.Text = message;
    }
}
