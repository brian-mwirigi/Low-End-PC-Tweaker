using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Tweakwell.App;

internal static class Theme
{
    public static readonly Color Bg = Color.FromArgb(255, 0, 0, 0);
    public static readonly Color Surface = Color.FromArgb(255, 16, 16, 16);
    public static readonly Color SurfaceRaised = Color.FromArgb(255, 28, 28, 28);
    public static readonly Color Line = Color.FromArgb(255, 38, 38, 38);
    public static readonly Color Text = Color.FromArgb(255, 240, 234, 224);
    public static readonly Color Muted = Color.FromArgb(255, 150, 142, 128);
    public static readonly Color Amber = Color.FromArgb(255, 214, 158, 74);
    public static readonly Color Caution = Color.FromArgb(255, 214, 132, 64);
    public static readonly Color Sage = Color.FromArgb(255, 138, 160, 118);

    public static FontFamily Sans { get; } = new("Segoe UI");
    public static FontFamily Mono { get; } = new("Consolas");

    public static SolidColorBrush Brush(Color color) => new(color);

    public static Border Chip(string text, Color foreground, Color background)
    {
        return new Border
        {
            Background = Brush(background),
            CornerRadius = new CornerRadius(2),
            Padding = new Thickness(7, 1, 7, 2),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = text.ToUpperInvariant(),
                FontSize = 10,
                CharacterSpacing = 40,
                FontFamily = Sans,
                Foreground = Brush(foreground),
            },
        };
    }

    public static Border Card(UIElement child, Thickness? padding = null)
    {
        return new Border
        {
            Background = Brush(Surface),
            BorderBrush = Brush(Line),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(2),
            Padding = padding ?? new Thickness(16),
            Child = child,
        };
    }

    public static TextBlock Label(string text) => new()
    {
        Text = text,
        FontSize = 10,
        CharacterSpacing = 90,
        FontFamily = Sans,
        Foreground = Brush(Muted),
    };

    public static TextBlock Body(string text, bool muted = false) => new()
    {
        Text = text,
        FontSize = 13,
        FontFamily = Sans,
        Foreground = Brush(muted ? Muted : Text),
        TextWrapping = TextWrapping.Wrap,
        LineHeight = 20,
    };

    public static TextBlock Title(string text, double size = 20) => new()
    {
        Text = text,
        FontSize = size,
        FontWeight = FontWeights.SemiBold,
        FontFamily = Sans,
        Foreground = Brush(Text),
        TextWrapping = TextWrapping.Wrap,
    };

    public static TextBlock MonoText(string text, double size = 12, bool muted = false) => new()
    {
        Text = text,
        FontSize = size,
        FontFamily = Mono,
        Foreground = Brush(muted ? Muted : Text),
        TextWrapping = TextWrapping.Wrap,
    };

    public static UIElement Stat(string value, string label, string? hint = null)
    {
        var stack = new StackPanel { Spacing = 4 };
        stack.Children.Add(new TextBlock
        {
            Text = value,
            FontSize = 40,
            FontWeight = FontWeights.Light,
            FontFamily = Sans,
            Foreground = Brush(Text),
            TextWrapping = TextWrapping.NoWrap,
        });
        stack.Children.Add(Label(label.ToUpperInvariant()));
        if (!string.IsNullOrWhiteSpace(hint))
        {
            stack.Children.Add(MonoText(hint, 11, muted: true));
        }

        return stack;
    }

    public static Border Spec(string label, string value, string? hint = null)
    {
        var stack = new StackPanel { Spacing = 8 };
        stack.Children.Add(Label(label.ToUpperInvariant()));
        stack.Children.Add(new TextBlock
        {
            Text = value,
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            FontFamily = Sans,
            Foreground = Brush(Text),
            TextWrapping = TextWrapping.Wrap,
        });
        if (!string.IsNullOrWhiteSpace(hint))
        {
            stack.Children.Add(MonoText(hint, 11, muted: true));
        }

        return Card(stack, new Thickness(16, 14, 16, 16));
    }

    public static Border Finding(Finding finding)
    {
        var warning = finding.Severity == FindingSeverity.Warning;
        var stack = new StackPanel { Spacing = 4 };
        stack.Children.Add(Chip(warning ? "Watch" : "Note", warning ? Bg : Muted, warning ? Caution : SurfaceRaised));
        stack.Children.Add(Title(finding.Title, 15));
        stack.Children.Add(Body(finding.Detail, muted: true));

        var row = new Grid { ColumnSpacing = 14 };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var rail = new Border
        {
            Width = 2,
            Background = Brush(warning ? Caution : Amber),
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        Grid.SetColumn(rail, 0);
        Grid.SetColumn(stack, 1);
        row.Children.Add(rail);
        row.Children.Add(stack);

        return new Border
        {
            Background = Brush(Surface),
            BorderBrush = Brush(Line),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(2),
            Padding = new Thickness(14, 12, 16, 14),
            Child = row,
        };
    }

    public static Border StartupRow(StartupApp app)
    {
        var grid = new Grid { ColumnSpacing = 16 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var name = new TextBlock
        {
            Text = app.Name,
            FontSize = 14,
            FontFamily = Sans,
            Foreground = Brush(Text),
            VerticalAlignment = VerticalAlignment.Center,
        };
        var command = MonoText(ShortCommand(app.Command), 11, muted: true);
        command.TextTrimming = TextTrimming.CharacterEllipsis;
        command.VerticalAlignment = VerticalAlignment.Center;
        var meta = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
        meta.Children.Add(app.Enabled
            ? Chip("On", Bg, Sage)
            : Chip("Off", Muted, SurfaceRaised));
        meta.Children.Add(Chip(Scope(app.Source), Muted, SurfaceRaised));

        Grid.SetColumn(name, 0);
        Grid.SetColumn(command, 1);
        Grid.SetColumn(meta, 2);
        grid.Children.Add(name);
        grid.Children.Add(command);
        grid.Children.Add(meta);

        return new Border
        {
            Padding = new Thickness(4, 12, 4, 12),
            BorderBrush = Brush(Line),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = grid,
        };
    }

    public static UIElement Ledger(string when, string action, string id, string summary)
    {
        var head = new Grid { ColumnSpacing = 12 };
        head.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(132) });
        head.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        head.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var time = MonoText(when, 11, muted: true);
        var act = new TextBlock
        {
            Text = action.ToUpperInvariant(),
            FontSize = 10,
            CharacterSpacing = 60,
            FontFamily = Sans,
            Foreground = Brush(Amber),
            VerticalAlignment = VerticalAlignment.Center,
        };
        var key = MonoText(id, 11, muted: true);
        Grid.SetColumn(time, 0);
        Grid.SetColumn(act, 1);
        Grid.SetColumn(key, 2);
        head.Children.Add(time);
        head.Children.Add(act);
        head.Children.Add(key);

        var stack = new StackPanel { Spacing = 4, Margin = new Thickness(0, 10, 0, 10) };
        stack.Children.Add(head);
        stack.Children.Add(Body(summary));
        return stack;
    }

    public static Border ProofChange(PlannedChange change)
    {
        var stack = new StackPanel { Spacing = 8 };
        stack.Children.Add(Label(change.Target.ToUpperInvariant()));
        stack.Children.Add(MonoText(change.DisplayPath, 12, muted: true));

        var cols = new Grid { ColumnSpacing = 16 };
        cols.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        cols.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var was = new StackPanel { Spacing = 4 };
        was.Children.Add(Label("Was"));
        was.Children.Add(Body(change.OldValue, muted: true));
        var next = new StackPanel { Spacing = 4 };
        next.Children.Add(Label("Will be"));
        next.Children.Add(Body(change.NewValue));
        Grid.SetColumn(was, 0);
        Grid.SetColumn(next, 1);
        cols.Children.Add(was);
        cols.Children.Add(next);
        stack.Children.Add(cols);

        return new Border
        {
            BorderBrush = Brush(Line),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(0, 12, 0, 4),
            Child = stack,
        };
    }

    public static string ShortCommand(string command)
    {
        var path = command.Trim();
        if (path.StartsWith('"'))
        {
            var end = path.IndexOf('"', 1);
            if (end > 1)
            {
                path = path[1..end];
            }
        }
        else
        {
            var space = path.IndexOf(' ');
            if (space > 0)
            {
                path = path[..space];
            }
        }

        try
        {
            var name = Path.GetFileName(path);
            var folder = Path.GetFileName(Path.GetDirectoryName(path));
            if (string.IsNullOrEmpty(name))
            {
                return command;
            }

            return string.IsNullOrEmpty(folder) ? name : $@"{folder}\{name}";
        }
        catch (Exception)
        {
            return command;
        }
    }

    public static string Scope(StartupSource source) => source switch
    {
        StartupSource.CurrentUserRun => "This user",
        StartupSource.LocalMachineRun => "All users",
        StartupSource.StartupFolder => "Folder",
        _ => source.ToString(),
    };

    public static string Media(StorageMedia media) => media switch
    {
        StorageMedia.Ssd => "SSD",
        StorageMedia.Hdd => "HDD",
        _ => "Disk",
    };
}
