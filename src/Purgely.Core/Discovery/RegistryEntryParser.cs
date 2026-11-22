using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using Purgely.Core.Models;

namespace Purgely.Core.Discovery;

/// <summary>
/// Turns the values of one registry "Uninstall" subkey into an <see cref="InstalledApp"/>,
/// or null when the entry should not be shown (updates, hidden components, broken entries).
/// Pure function so it can be unit tested without touching the registry.
/// </summary>
public static partial class RegistryEntryParser
{
    [GeneratedRegex(@"msiexec(?:\.exe)?""?\s+.*?/[IX]\s*(\{[0-9A-F]{8}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{12}\})",
        RegexOptions.IgnoreCase)]
    private static partial Regex MsiExecCommand();

    public static InstalledApp? TryParse(string keyName, IReadOnlyDictionary<string, object?> values, RegistryLocation location)
    {
        string? Str(string name) =>
            values.TryGetValue(name, out var o) && o is string s && !string.IsNullOrWhiteSpace(s) ? s.Trim() : null;

        long? Num(string name) => values.TryGetValue(name, out var o)
            ? o switch
            {
                int i => i,
                long l => l,
                string s when long.TryParse(s, out var n) => n,
                _ => null,
            }
            : null;

        var name = Str("DisplayName");
        if (name is null) return null;
        if (Num("SystemComponent") == 1) return null;
        if (Str("ParentKeyName") is not null) return null; // child update/patch of another product

        var releaseType = Str("ReleaseType");
        if (releaseType is not null &&
            (releaseType.Contains("Update", StringComparison.OrdinalIgnoreCase) ||
             releaseType.Contains("Hotfix", StringComparison.OrdinalIgnoreCase)))
            return null;

        var uninstall = Str("UninstallString");
        var quiet = Str("QuietUninstallString");

        string? productCode = null;
        if (Num("WindowsInstaller") == 1 && Guid.TryParse(keyName, out _))
        {
            productCode = keyName;
        }
        else if (uninstall is not null && MsiExecCommand().Match(uninstall) is { Success: true } m)
        {
            productCode = m.Groups[1].Value;
        }

        if (productCode is null && uninstall is null && quiet is null) return null;

        var (iconPath, iconIndex) = ParseIcon(Str("DisplayIcon"));

        return new InstalledApp
        {
            Id = $"reg:{location.Hive}:{location.View}:{keyName}",
            Name = name,
            Kind = productCode is not null ? AppKind.Msi : AppKind.Win32,
            Scope = location.Hive == RegistryHive.CurrentUser ? InstallScope.User : InstallScope.Machine,
            Publisher = Str("Publisher"),
            Version = Str("DisplayVersion"),
            SizeBytes = Num("EstimatedSize") is long kb and > 0 ? kb * 1024 : null,
            InstallDate = ParseDate(Str("InstallDate")),
            InstallLocation = Str("InstallLocation")?.Trim('"'),
            IconPath = iconPath,
            IconIndex = iconIndex,
            UninstallCommand = uninstall,
            QuietUninstallCommand = quiet,
            ProductCode = productCode?.ToUpperInvariant(),
            Registry = location,
            NoRemove = Num("NoRemove") == 1,
        };
    }

    /// <summary>Parses DisplayIcon values like <c>"C:\App\app.exe",0</c> or <c>C:\App\app.ico</c>.</summary>
    internal static (string? Path, int Index) ParseIcon(string? displayIcon)
    {
        if (displayIcon is null) return (null, 0);

        var path = displayIcon;
        var index = 0;
        var comma = displayIcon.LastIndexOf(',');
        if (comma > 0 && int.TryParse(displayIcon[(comma + 1)..].Trim(), out var parsed))
        {
            path = displayIcon[..comma];
            index = parsed;
        }

        path = Environment.ExpandEnvironmentVariables(path.Trim().Trim('"'));
        return (path.Length == 0 ? null : path, index);
    }

    internal static DateTime? ParseDate(string? value) =>
        value is not null && DateTime.TryParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var date)
            ? date
            : null;
}
