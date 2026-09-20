using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Tweakwell.App;

internal static class Theme
{
    public static readonly Color Bg = Color.FromArgb(255, 0, 0, 0);
    public static readonly Color Surface = Color.FromArgb(255, 20, 20, 20);
    public static readonly Color SurfaceRaised = Color.FromArgb(255, 32, 32, 32);
    public static readonly Color Line = Color.FromArgb(255, 42, 42, 42);
    public static readonly Color Text = Color.FromArgb(255, 240, 234, 224);
    public static readonly Color Muted = Color.FromArgb(255, 158, 150, 136);
    public static readonly Color Amber = Color.FromArgb(255, 214, 158, 74);
    public static readonly Color Caution = Color.FromArgb(255, 214, 132, 64);
    public static readonly Color Sage = Color.FromArgb(255, 138, 160, 118);

    public static SolidColorBrush Brush(Color color) => new(color);

    public static Border Chip(string text, Color foreground, Color background)
    {
        return new Border
        {
            Background = Brush(background),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8, 2, 8, 3),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = text,
                FontSize = 11,
                FontFamily = new FontFamily("Segoe UI"),
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
            CornerRadius = new CornerRadius(6),
            Padding = padding ?? new Thickness(16),
            Child = child,
        };
    }

    public static TextBlock Label(string text) => new()
    {
        Text = text,
        FontSize = 11,
        CharacterSpacing = 40,
        FontFamily = new FontFamily("Segoe UI"),
        Foreground = Brush(Muted),
    };

    public static TextBlock Body(string text, bool muted = false) => new()
    {
        Text = text,
        FontSize = 14,
        FontFamily = new FontFamily("Segoe UI"),
        Foreground = Brush(muted ? Muted : Text),
        TextWrapping = TextWrapping.Wrap,
    };

    public static TextBlock Title(string text, double size = 20) => new()
    {
        Text = text,
        FontSize = size,
        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
        FontFamily = new FontFamily("Segoe UI"),
        Foreground = Brush(Text),
        TextWrapping = TextWrapping.Wrap,
    };

    public static Border Spec(string label, string value, string? hint = null)
    {
        var stack = new StackPanel { Spacing = 6 };
        stack.Children.Add(Label(label.ToUpperInvariant()));
        stack.Children.Add(new TextBlock
        {
            Text = value,
            FontSize = 15,
            FontFamily = new FontFamily("Segoe UI"),
            Foreground = Brush(Text),
            TextWrapping = TextWrapping.Wrap,
        });
        if (!string.IsNullOrWhiteSpace(hint))
        {
            stack.Children.Add(Body(hint, muted: true));
        }

        return Card(stack, new Thickness(16, 14, 16, 14));
    }

    public static Border Finding(Finding finding)
    {
        var warning = finding.Severity == FindingSeverity.Warning;
        var stack = new StackPanel { Spacing = 6 };
        stack.Children.Add(Chip(warning ? "Watch" : "Note", warning ? Bg : Text, warning ? Caution : SurfaceRaised));
        stack.Children.Add(Title(finding.Title, 15));
        stack.Children.Add(Body(finding.Detail, muted: true));
        return Card(stack);
    }

    public static Border StartupRow(StartupApp app)
    {
        var grid = new Grid { ColumnSpacing = 12 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var text = new StackPanel { Spacing = 2 };
        text.Children.Add(new TextBlock
        {
            Text = app.Name,
            FontSize = 14,
            FontFamily = new FontFamily("Segoe UI"),
            Foreground = Brush(Text),
        });
        text.Children.Add(new TextBlock
        {
            Text = ShortCommand(app.Command),
            FontSize = 11,
            FontFamily = new FontFamily("Segoe UI"),
            Foreground = Brush(Muted),
            TextTrimming = TextTrimming.CharacterEllipsis,
        });

        var meta = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
        meta.Children.Add(app.Enabled
            ? Chip("On", Bg, Sage)
            : Chip("Off", Muted, SurfaceRaised));
        meta.Children.Add(Chip(Scope(app.Source), Muted, SurfaceRaised));

        Grid.SetColumn(text, 0);
        Grid.SetColumn(meta, 1);
        grid.Children.Add(text);
        grid.Children.Add(meta);

        return new Border
        {
            Padding = new Thickness(4, 10, 4, 10),
            BorderBrush = Brush(Line),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = grid,
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
