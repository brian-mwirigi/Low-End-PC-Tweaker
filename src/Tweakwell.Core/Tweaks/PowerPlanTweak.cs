namespace Tweakwell;

public sealed class PowerPlanTweak : ITweak
{
    private readonly IProcessRunner _runner;
    private readonly IElevatedOperations _elevated;

    public PowerPlanTweak(IProcessRunner runner, IElevatedOperations elevated, bool isLaptop)
    {
        _runner = runner;
        _elevated = elevated;
        IsLaptop = isLaptop;
    }

    public bool IsLaptop { get; set; }

    public string Id => "power-plan";
    public string Title => "Switch to the high-performance power plan";
    public string Description => IsLaptop
        ? "Sets the High performance scheme (8c5e7fda-…). On a laptop this drains the battery faster and can run the fans harder. Undo puts the previous scheme back."
        : "Sets the High performance scheme (8c5e7fda-…). Undo puts the previous scheme back.";
    public TweakRisk Risk => TweakRisk.Caution;
    public bool RequiresAdmin => true;
    public string? AdminReason => "Change the system power plan with powercfg.";
    public bool IsReversible => true;
    public bool IsSelected { get; set; }

    public IReadOnlyList<PlannedChange> Preview()
    {
        var (name, guid) = new PowerPlanReader(_runner).Active();
        var next = PowerPlanReader.HighPerformance.ToString("D");
        return
        [
            new PlannedChange("PowerCfg", "active scheme", null, $"{name} ({guid})", $"High performance ({next})"),
        ];
    }

    public void Apply()
    {
        var result = _elevated.SetPowerPlan(PowerPlanReader.HighPerformance);
        if (!result.Ok)
        {
            throw new InvalidOperationException(result.Message);
        }
    }

    public void Undo(IReadOnlyList<PlannedChange> previous)
    {
        var old = previous.FirstOrDefault()?.OldValue ?? "";
        var guid = ExtractGuid(old);
        if (guid is null)
        {
            throw new InvalidOperationException("Previous power plan GUID was not stored.");
        }

        var result = _elevated.SetPowerPlan(guid.Value);
        if (!result.Ok)
        {
            throw new InvalidOperationException(result.Message);
        }
    }

    internal static Guid? ExtractGuid(string text)
    {
        var start = text.LastIndexOf('(');
        var end = text.LastIndexOf(')');
        if (start < 0 || end <= start)
        {
            return Guid.TryParse(text, out var direct) ? direct : null;
        }

        return Guid.TryParse(text[(start + 1)..end], out var id) ? id : null;
    }
}
