using System.Diagnostics;

namespace Tweakwell;

public sealed class ProcessRunner : IProcessRunner
{
    public ProcessRunResult Run(string fileName, string arguments, int timeoutMs = 15_000)
    {
        var start = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        using var process = Process.Start(start)
                            ?? throw new InvalidOperationException($"Could not start {fileName}.");
        if (!process.WaitForExit(timeoutMs))
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (Exception)
            {
                // Best effort.
            }

            return new ProcessRunResult(-1, "", $"Timed out after {timeoutMs} ms.");
        }

        return new ProcessRunResult(process.ExitCode, process.StandardOutput.ReadToEnd(), process.StandardError.ReadToEnd());
    }
}
