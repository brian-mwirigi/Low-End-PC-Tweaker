using System.Diagnostics;

namespace Tweakwell;

public sealed class ProcessRunner : IProcessRunner
{
    public ProcessRunResult Run(string fileName, string arguments, int timeoutMs = 3_000)
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
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
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

        stdout.Wait(500);
        stderr.Wait(500);
        return new ProcessRunResult(process.ExitCode, stdout.IsCompletedSuccessfully ? stdout.Result : "", stderr.IsCompletedSuccessfully ? stderr.Result : "");
    }
}
