using Microsoft.Win32;

namespace Tweakwell;

public sealed class StartupEnumerator
{
    public const string RunPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    public const string ApprovedRun = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
    public const string ApprovedRun32 = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run32";
    public const string ApprovedFolder = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\StartupFolder";

    private readonly IRegistry _registry;
    private readonly IFileSystem _files;

    public StartupEnumerator(IRegistry registry, IFileSystem files)
    {
        _registry = registry;
        _files = files;
    }

    public IReadOnlyList<StartupApp> List()
    {
        var apps = new List<StartupApp>();
        AddRun(apps, RegistryHive.CurrentUser, StartupSource.CurrentUserRun, canDisable: true);
        AddRun(apps, RegistryHive.LocalMachine, StartupSource.LocalMachineRun, canDisable: false);
        AddStartupFolder(apps);
        return apps;
    }

    private void AddRun(List<StartupApp> apps, RegistryHive hive, StartupSource source, bool canDisable)
    {
        foreach (var name in _registry.GetValueNames(hive, RunPath))
        {
            var command = _registry.GetString(hive, RunPath, name) ?? "";
            var enabled = IsEnabled(hive, name);
            apps.Add(new StartupApp(name, command, source, enabled, CanDisable: canDisable));
        }
    }

    private void AddStartupFolder(List<StartupApp> apps)
    {
        var folder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        if (!_files.DirectoryExists(folder))
        {
            return;
        }

        foreach (var file in _files.EnumerateFiles(folder, recursive: false))
        {
            var name = Path.GetFileName(file);
            if (name.Equals("desktop.ini", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var enabled = IsFolderItemEnabled(name);
            apps.Add(new StartupApp(name, file, StartupSource.StartupFolder, enabled, CanDisable: true));
        }
    }

    public bool IsEnabled(RegistryHive hive, string valueName)
    {
        var blob = _registry.GetBinary(hive, ApprovedRun, valueName)
                   ?? _registry.GetBinary(hive, ApprovedRun32, valueName);
        return DecodeEnabled(blob);
    }

    public bool IsFolderItemEnabled(string fileName)
    {
        var blob = _registry.GetBinary(RegistryHive.CurrentUser, ApprovedFolder, fileName);
        return DecodeEnabled(blob);
    }

    public static bool DecodeEnabled(byte[]? blob)
    {
        if (blob is null || blob.Length == 0)
        {
            return true;
        }

        return blob[0] != 0x03;
    }

    public static byte[] DisabledBlob(DateTimeOffset when)
    {
        var fileTime = when.ToFileTime();
        var bytes = new byte[12];
        bytes[0] = 0x03;
        BitConverter.GetBytes(fileTime).CopyTo(bytes, 4);
        return bytes;
    }

    public static byte[] EnabledBlob() => [0x02, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];

    public static string ApprovedPathFor(StartupSource source) => source switch
    {
        StartupSource.StartupFolder => ApprovedFolder,
        _ => ApprovedRun,
    };
}
