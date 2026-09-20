namespace Tweakwell;

public interface IElevatedOperations
{
    ElevatedResult CreateRestorePoint(string description);
    ElevatedResult SetPowerPlan(Guid schemeId);
}

public sealed record ElevatedResult(bool Ok, string Message);
