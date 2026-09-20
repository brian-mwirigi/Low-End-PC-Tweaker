using Microsoft.Win32;

namespace Tweakwell.Tests;

public sealed class StartupAndGpuTests
{
    [Fact]
    public void StartupApproved_DisabledBlob_IsFlaggedDisabled()
    {
        var blob = StartupEnumerator.DisabledBlob(DateTimeOffset.UnixEpoch);
        Assert.False(StartupEnumerator.DecodeEnabled(blob));
        Assert.True(StartupEnumerator.DecodeEnabled(StartupEnumerator.EnabledBlob()));
        Assert.True(StartupEnumerator.DecodeEnabled(null));
    }

    [Fact]
    public void StartupAppsTweak_SetsApprovedBinary_WithoutDeletingRunValue()
    {
        var registry = new FakeRegistry();
        registry.SetString(RegistryHive.CurrentUser, StartupEnumerator.RunPath, "Discord", "discord.exe");
        var tweak = new StartupAppsTweak(registry) { IsSelected = true };
        tweak.Selected.Add(new StartupPick("Discord", StartupSource.CurrentUserRun));

        tweak.Apply();
        Assert.Equal("discord.exe", registry.GetString(RegistryHive.CurrentUser, StartupEnumerator.RunPath, "Discord"));
        var blob = registry.GetBinary(RegistryHive.CurrentUser, StartupEnumerator.ApprovedRun, "Discord");
        Assert.NotNull(blob);
        Assert.False(StartupEnumerator.DecodeEnabled(blob));
    }

    [Fact]
    public void DedicatedGpuTweak_WritesPreference_ForExistingSafeExe()
    {
        var exe = Path.Combine(Path.GetTempPath(), $"tweakwell-{Guid.NewGuid():N}.exe");
        File.WriteAllBytes(exe, [0x4D, 0x5A]);
        try
        {
            var registry = new FakeRegistry();
            var tweak = new DedicatedGpuTweak(registry) { IsSelected = true, ExecutablePath = exe };
            var preview = tweak.Preview();
            Assert.Equal(DedicatedGpuTweak.HighPerformance, preview[0].NewValue);
            tweak.Apply();
            Assert.Equal(DedicatedGpuTweak.HighPerformance, registry.GetString(RegistryHive.CurrentUser, DedicatedGpuTweak.Key, Path.GetFullPath(exe)));
        }
        finally
        {
            File.Delete(exe);
        }
    }

    [Fact]
    public void DedicatedGpuTweak_RefusesAntiCheat()
    {
        var registry = new FakeRegistry();
        var tweak = new DedicatedGpuTweak(registry)
        {
            ExecutablePath = @"C:\Program Files\EasyAntiCheat\EasyAntiCheat.exe",
        };
        Assert.Throws<InvalidOperationException>(() => tweak.Preview());
    }

    [Fact]
    public void WmiHardwareProbe_ClassifiesIgpusAndDgpus()
    {
        Assert.Equal(GpuKind.Integrated, WmiHardwareProbe.ClassifyGpu("Intel(R) UHD Graphics"));
        Assert.Equal(GpuKind.Dedicated, WmiHardwareProbe.ClassifyGpu("NVIDIA GeForce GTX 1650"));
        Assert.Equal(GpuKind.Dedicated, WmiHardwareProbe.ClassifyGpu("AMD Radeon RX 580"));
        Assert.Equal(StorageMedia.Hdd, WmiHardwareProbe.ParseMedia("3"));
        Assert.Equal(StorageMedia.Ssd, WmiHardwareProbe.ParseMedia("4"));
    }

    [Fact]
    public void PowerPlanTweak_ExtractsGuidFromPreviewText()
    {
        var guid = PowerPlanTweak.ExtractGuid("Balanced (381b4222-f694-41f0-9685-ff5bb260df2e)");
        Assert.Equal(PowerPlanReader.Balanced, guid);
    }
}
