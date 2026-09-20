using Microsoft.Win32;

namespace Tweakwell.Tests;

public sealed class ChangeEngineTests
{
    [Fact]
    public void Apply_WritesBackupAndChangelog_ThenUndoRestores()
    {
        var root = Path.Combine(Path.GetTempPath(), "tweakwell-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var store = new LocalStore(root);
            var elevated = new FakeElevated();
            var engine = new ChangeEngine(store, elevated);
            var registry = new FakeRegistry();
            registry.SetDword(RegistryHive.CurrentUser, GameModeTweak.Key, "AutoGameModeEnabled", 0);
            var tweak = new GameModeTweak(registry) { IsSelected = true };

            var plan = engine.Preview([tweak]);
            Assert.NotEmpty(plan.AllChanges);
            Assert.True(plan.NeedsRestorePoint);
            Assert.Contains(plan.AdminReasons, r => r.Contains("restore point", StringComparison.OrdinalIgnoreCase));

            var outcome = engine.Apply(plan, createRestorePoint: true);
            Assert.True(outcome.Applied);
            Assert.True(outcome.ShowTip);
            Assert.Equal(1, registry.GetDword(RegistryHive.CurrentUser, GameModeTweak.Key, "AutoGameModeEnabled"));
            Assert.Contains(elevated.Calls, c => c.StartsWith("restore:"));

            var errors = engine.UndoTweak(tweak.Id, [tweak]);
            Assert.Empty(errors);
            Assert.Equal(0, registry.GetDword(RegistryHive.CurrentUser, GameModeTweak.Key, "AutoGameModeEnabled"));
            Assert.Contains(store.ReadLog(), e => e.Action == "apply");
            Assert.Contains(store.ReadLog(), e => e.Action == "undo");
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch (Exception) { /* test cleanup */ }
        }
    }

    [Fact]
    public void Preview_DoesNotWriteRegistry()
    {
        var store = new LocalStore(Path.Combine(Path.GetTempPath(), "tweakwell-tests", Guid.NewGuid().ToString("N")));
        var engine = new ChangeEngine(store, new FakeElevated());
        var registry = new FakeRegistry();
        var tweak = new GameModeTweak(registry) { IsSelected = true };

        _ = engine.Preview([tweak]);
        Assert.Null(registry.GetDword(RegistryHive.CurrentUser, GameModeTweak.Key, "AutoGameModeEnabled"));
    }
}
