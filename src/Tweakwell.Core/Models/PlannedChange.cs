namespace Tweakwell;

public sealed record PlannedChange(
    string Target,
    string Path,
    string? ValueName,
    string OldValue,
    string NewValue)
{
    public string DisplayPath => string.IsNullOrEmpty(ValueName) ? Path : $"{Path}\\{ValueName}";
}
