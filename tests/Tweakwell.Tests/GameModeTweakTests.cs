using Microsoft.Win32;

namespace Tweakwell.Tests;

public sealed class GameModeTweakTests
{
    [Fact]
    public void PreviewApplyUndo_RestoresPreviousDwords()
    {
        var registry = new FakeRegistry();
        registry.SetDword(RegistryHive.CurrentUser, GameModeTweak.Key, "AutoGameModeEnabled", 0);
        var tweak = new GameModeTweak(registry) { IsSelected = true };

        var preview = tweak.Preview();
        Assert.Equal("0", preview[0].OldValue);
        Assert.Equal("1", preview[0].NewValue);

        tweak.Apply();
        Assert.Equal(1, registry.GetDword(RegistryHive.CurrentUser, GameModeTweak.Key, "AutoGameModeEnabled"));

        tweak.Undo(preview);
        Assert.Equal(0, registry.GetDword(RegistryHive.CurrentUser, GameModeTweak.Key, "AutoGameModeEnabled"));
    }
}
