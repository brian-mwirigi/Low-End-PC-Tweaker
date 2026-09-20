namespace Tweakwell.Tests;

public sealed class TempCleanTweakTests
{
    [Fact]
    public void Preview_CountsFiles_ThenApplyDeletesThem()
    {
        var files = new FakeFileSystem();
        var temp = @"C:\Users\test\AppData\Local\Temp";
        files.Directories.Add(temp);
        files.Files[Path.Combine(temp, "a.tmp")] = new byte[100];
        files.Files[Path.Combine(temp, "b.tmp")] = new byte[20];

        var tweak = new TempCleanTweak(files) { IsSelected = true };
        var preview = tweak.Preview();

        Assert.Single(preview);
        Assert.Contains("2 files", preview[0].OldValue);
        Assert.Contains("120 B", preview[0].OldValue);
        Assert.Contains("cannot undo", preview[0].NewValue);

        tweak.Apply();
        Assert.Equal(2, files.Deleted.Count);
        Assert.Empty(files.Files);
    }

    [Fact]
    public void Preview_MissingFolder_DoesNotThrow()
    {
        var tweak = new TempCleanTweak(new FakeFileSystem()) { IsSelected = true };
        var preview = tweak.Preview();
        Assert.Contains("missing", preview[0].OldValue);
    }

    [Fact]
    public void Summarize_CapsAtMaxFiles()
    {
        var files = new FakeFileSystem();
        var temp = @"C:\Users\test\AppData\Local\Temp";
        files.Directories.Add(temp);
        for (var i = 0; i < 40; i++)
        {
            files.Files[Path.Combine(temp, $"f{i}.tmp")] = [1];
        }

        var walk = files.Summarize(temp, maxFiles: 10, timeoutMs: 1_000);
        Assert.Equal(10, walk.Files);
        Assert.True(walk.Capped);
        Assert.Equal(10, walk.Bytes);
    }

    [Fact]
    public void WindowsFileSystem_Summarize_CapsAndLeavesFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), "tweakwell-walk-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            for (var i = 0; i < 30; i++)
            {
                File.WriteAllBytes(Path.Combine(root, $"f{i}.tmp"), new byte[8]);
            }

            var fs = new WindowsFileSystem();
            var walk = fs.Summarize(root, maxFiles: 12, timeoutMs: 2_000);
            Assert.Equal(12, walk.Files);
            Assert.True(walk.Capped);
            Assert.Equal(96, walk.Bytes);
            Assert.Equal(30, Directory.GetFiles(root).Length);
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch (Exception) { /* test cleanup */ }
        }
    }
}
