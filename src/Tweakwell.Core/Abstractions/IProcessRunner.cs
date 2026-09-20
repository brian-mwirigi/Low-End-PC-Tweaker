namespace Tweakwell;

public interface IProcessRunner
{
    ProcessRunResult Run(string fileName, string arguments, int timeoutMs = 15_000);
}

public sealed record ProcessRunResult(int ExitCode, string StandardOutput, string StandardError);
