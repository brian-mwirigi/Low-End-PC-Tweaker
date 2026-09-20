namespace Tweakwell;

public interface ITweak
{
    string Id { get; }
    string Title { get; }
    string Description { get; }
    TweakRisk Risk { get; }
    bool RequiresAdmin { get; }
    string? AdminReason { get; }
    bool IsReversible { get; }
    bool IsSelected { get; set; }

    IReadOnlyList<PlannedChange> Preview();
    void Apply();
    void Undo(IReadOnlyList<PlannedChange> previous);
}
