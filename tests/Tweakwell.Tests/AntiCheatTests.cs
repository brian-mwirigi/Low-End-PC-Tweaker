namespace Tweakwell.Tests;

public sealed class AntiCheatTests
{
    [Theory]
    [InlineData(@"C:\Program Files\EasyAntiCheat\EasyAntiCheat.exe")]
    [InlineData(@"C:\Games\Foo\BattlEye\BEService.exe")]
    [InlineData(@"C:\Program Files\Riot Vanguard\vgc.exe")]
    public void DeniesAntiCheatPaths(string path)
    {
        Assert.True(AntiCheat.IsDenied(path, out var reason));
        Assert.False(string.IsNullOrWhiteSpace(reason));
        Assert.False(AntiCheat.IsSafeGameExecutable(path, out _));
    }

    [Fact]
    public void AllowsOrdinaryGameExePath()
    {
        var path = @"C:\Games\LowEndHero\game.exe";
        Assert.False(AntiCheat.IsDenied(path, out _));
        Assert.True(AntiCheat.IsSafeGameExecutable(path, out _));
    }
}
