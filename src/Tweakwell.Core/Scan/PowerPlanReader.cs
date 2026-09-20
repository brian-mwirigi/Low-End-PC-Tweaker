using System.Text.RegularExpressions;

namespace Tweakwell;

public sealed class PowerPlanReader
{
    public static readonly Guid HighPerformance = Guid.Parse("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
    public static readonly Guid Balanced = Guid.Parse("381b4222-f694-41f0-9685-ff5bb260df2e");
    public static readonly Guid PowerSaver = Guid.Parse("a1841308-3541-4fab-bc81-f71556f20b4a");

    private static readonly Regex SchemeLine = new(
        @"GUID:\s*([0-9a-fA-F-]{36})\s*\((.+)\)",
        RegexOptions.Compiled);

    private readonly IProcessRunner _runner;

    public PowerPlanReader(IProcessRunner runner)
    {
        _runner = runner;
    }

    public (string Name, string Guid) Active()
    {
        var result = _runner.Run("powercfg", "/getactivescheme");
        var match = SchemeLine.Match(result.StandardOutput);
        if (!match.Success)
        {
            return ("Unknown", "");
        }

        return (match.Groups[2].Value.Trim(), match.Groups[1].Value);
    }

    public static string FriendlyName(string guid, string fallback)
    {
        if (!Guid.TryParse(guid, out var id))
        {
            return fallback;
        }

        if (id == HighPerformance) return "High performance";
        if (id == Balanced) return "Balanced";
        if (id == PowerSaver) return "Power saver";
        return fallback;
    }
}
