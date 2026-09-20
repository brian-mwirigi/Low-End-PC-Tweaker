using System.Diagnostics;
using System.Management;
using System.Text.Json;
using Tweakwell;

namespace Tweakwell.Elevated;

internal static class Program
{
    private static readonly HashSet<string> Verbs = new(StringComparer.OrdinalIgnoreCase)
    {
        "create-restore-point",
        "set-power-plan",
    };

    [STAThread]
    public static int Main(string[] args)
    {
        if (args.Length == 0 || !Verbs.Contains(args[0]))
        {
            return Write(false, null, "Allowed operations: create-restore-point, set-power-plan.", null);
        }

        var parsed = Parse(args);
        if (parsed.ResultPath is not null && !AppPaths.IsAllowedHelperResultPath(parsed.ResultPath))
        {
            return 2;
        }

        try
        {
            return parsed.Verb switch
            {
                "create-restore-point" => CreateRestorePoint(parsed.Positional.FirstOrDefault() ?? "Tweakwell before apply", parsed.ResultPath),
                "set-power-plan" => SetPowerPlan(parsed.Positional.FirstOrDefault() ?? "", parsed.ResultPath),
                _ => Write(false, null, "Unknown operation.", parsed.ResultPath),
            };
        }
        catch (Exception ex)
        {
            return Write(false, null, ex.Message, parsed.ResultPath);
        }
    }

    private static int CreateRestorePoint(string description, string? resultPath)
    {
        using var cls = new ManagementClass(@"root\default", "SystemRestore", new ObjectGetOptions());
        using var inParams = cls.GetMethodParameters("CreateRestorePoint");
        inParams["Description"] = description;
        inParams["RestorePointType"] = 12; // MODIFY_SETTINGS
        inParams["EventType"] = 100; // BEGIN_SYSTEM_CHANGE
        using var output = cls.InvokeMethod("CreateRestorePoint", inParams, null);
        var code = Convert.ToInt32(output?["ReturnValue"] ?? -1);
        return code == 0
            ? Write(true, "Restore point created.", null, resultPath)
            : Write(false, null, $"System Restore returned {code}. System Protection may be off.", resultPath);
    }

    private static int SetPowerPlan(string guidText, string? resultPath)
    {
        if (!Guid.TryParse(guidText, out var guid))
        {
            return Write(false, null, "set-power-plan requires a GUID.", resultPath);
        }

        var start = new ProcessStartInfo
        {
            FileName = "powercfg",
            Arguments = $"/setactive {guid:D}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        using var process = Process.Start(start);
        if (process is null)
        {
            return Write(false, null, "Could not start powercfg.", resultPath);
        }

        process.WaitForExit(15_000);
        if (process.ExitCode != 0)
        {
            var err = process.StandardError.ReadToEnd() + process.StandardOutput.ReadToEnd();
            return Write(false, null, string.IsNullOrWhiteSpace(err) ? $"powercfg exited {process.ExitCode}." : err.Trim(), resultPath);
        }

        return Write(true, $"Active power plan is now {guid:D}.", null, resultPath);
    }

    private static Parsed Parse(string[] args)
    {
        string? result = null;
        var positional = new List<string>();
        for (var i = 1; i < args.Length; i++)
        {
            if (args[i] is "--result" && i + 1 < args.Length)
            {
                result = args[++i];
                continue;
            }

            positional.Add(args[i]);
        }

        return new Parsed(args[0], positional, result);
    }

    private static int Write(bool ok, string? message, string? error, string? resultPath)
    {
        var json = JsonSerializer.Serialize(new { ok, message, error });
        if (!string.IsNullOrWhiteSpace(resultPath) && AppPaths.IsAllowedHelperResultPath(resultPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(resultPath)!);
            File.WriteAllText(resultPath, json);
        }

        return ok ? 0 : 1;
    }

    private sealed record Parsed(string Verb, List<string> Positional, string? ResultPath);
}
