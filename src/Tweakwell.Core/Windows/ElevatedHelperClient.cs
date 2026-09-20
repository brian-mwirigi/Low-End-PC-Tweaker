using System.Diagnostics;
using System.Text.Json;

namespace Tweakwell;

public sealed class ElevatedHelperClient : IElevatedOperations
{
    private readonly string _helperPath;

    public ElevatedHelperClient(string? helperPath = null)
    {
        _helperPath = helperPath ?? LocateHelper();
    }

    public ElevatedResult CreateRestorePoint(string description)
        => Invoke("create-restore-point", Quote(description));

    public ElevatedResult SetPowerPlan(Guid schemeId)
        => Invoke("set-power-plan", schemeId.ToString("D"));

    private ElevatedResult Invoke(string verb, string extraArgs)
    {
        if (!File.Exists(_helperPath))
        {
            return new ElevatedResult(false, $"Elevated helper not found at {_helperPath}.");
        }

        AppPaths.EnsureCreated();
        var resultPath = Path.Combine(AppPaths.Root, $"elevated-{Guid.NewGuid():N}.json");
        var args = $"{verb} {extraArgs} --result {Quote(resultPath)}".Trim();

        try
        {
            var start = new ProcessStartInfo
            {
                FileName = _helperPath,
                Arguments = args,
                UseShellExecute = true,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden,
            };

            using var process = Process.Start(start);
            if (process is null)
            {
                return new ElevatedResult(false, "Could not start the elevated helper.");
            }

            process.WaitForExit(60_000);
            if (!File.Exists(resultPath))
            {
                return new ElevatedResult(false, process.ExitCode == 0
                    ? "Helper finished but wrote no result (UAC may have been cancelled)."
                    : "Administrator approval was declined or the helper failed.");
            }

            var json = File.ReadAllText(resultPath);
            var parsed = JsonSerializer.Deserialize<HelperFile>(json);
            return parsed is null
                ? new ElevatedResult(false, "Helper result was not valid JSON.")
                : new ElevatedResult(parsed.Ok, parsed.Message ?? parsed.Error ?? "");
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return new ElevatedResult(false, "Administrator approval was declined.");
        }
        finally
        {
            try
            {
                if (File.Exists(resultPath))
                {
                    File.Delete(resultPath);
                }
            }
            catch (Exception)
            {
                // leftover temp file is harmless
            }
        }
    }

    private static string Quote(string value) => $"\"{value.Replace("\"", "\\\"")}\"";

    internal static string LocateHelper()
    {
        var names = new[] { "Tweakwell.Elevated.exe" };
        var dirs = new[]
        {
            AppContext.BaseDirectory,
            Path.GetDirectoryName(Environment.ProcessPath) ?? "",
        };

        foreach (var dir in dirs)
        {
            foreach (var name in names)
            {
                var candidate = Path.Combine(dir, name);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        return Path.Combine(AppContext.BaseDirectory, "Tweakwell.Elevated.exe");
    }

    private sealed class HelperFile
    {
        public bool Ok { get; set; }
        public string? Message { get; set; }
        public string? Error { get; set; }
    }
}
