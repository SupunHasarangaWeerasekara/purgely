using Microsoft.Win32;
using Purgely.Core.Models;

namespace Purgely.Core.Discovery;

/// <summary>Reads Win32 and MSI programs from the registry "Uninstall" keys.</summary>
public sealed class RegistryAppSource : IAppSource
{
    private const string UninstallPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

    private static readonly (RegistryHive Hive, RegistryView View)[] Locations =
    [
        (RegistryHive.LocalMachine, RegistryView.Registry64),
        (RegistryHive.LocalMachine, RegistryView.Registry32), // WOW6432Node: 32-bit programs
        (RegistryHive.CurrentUser, RegistryView.Default),     // per-user installs
    ];

    public IReadOnlyList<InstalledApp> GetApps()
    {
        var apps = new List<InstalledApp>();
        foreach (var (hive, view) in Locations)
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, view);
                using var root = baseKey.OpenSubKey(UninstallPath);
                if (root is null) continue;

                foreach (var keyName in root.GetSubKeyNames())
                {
                    var app = TryReadEntry(root, keyName, new RegistryLocation(hive, view, $@"{UninstallPath}\{keyName}"));
                    if (app is not null) apps.Add(app);
                }
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException)
            {
                // Skip locations we cannot read.
            }
        }

        return apps;
    }

    public bool Handles(InstalledApp app) => app.Registry is not null;

    public bool IsInstalled(InstalledApp app)
    {
        if (app.Registry is not { } loc) return false;
        using var baseKey = RegistryKey.OpenBaseKey(loc.Hive, loc.View);
        using var key = baseKey.OpenSubKey(loc.SubKeyPath);
        return key is not null;
    }

    private static InstalledApp? TryReadEntry(RegistryKey root, string keyName, RegistryLocation location)
    {
        try
        {
            using var key = root.OpenSubKey(keyName);
            if (key is null) return null;

            var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var valueName in key.GetValueNames())
            {
                values[valueName] = key.GetValue(valueName);
            }

            return RegistryEntryParser.TryParse(keyName, values, location);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException)
        {
            return null;
        }
    }
}
