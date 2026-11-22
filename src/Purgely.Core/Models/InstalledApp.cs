using Microsoft.Win32;

namespace Purgely.Core.Models;

public enum AppKind
{
    /// <summary>Classic desktop program with its own uninstaller (NSIS, Inno Setup, ...).</summary>
    Win32,
    /// <summary>Windows Installer package, removed with msiexec.</summary>
    Msi,
    /// <summary>Store / MSIX / UWP package, removed with PackageManager.</summary>
    Msix,
}

public enum InstallScope
{
    Machine,
    User,
}

/// <summary>Where a Win32/MSI app's uninstall entry lives in the registry.</summary>
public sealed record RegistryLocation(RegistryHive Hive, RegistryView View, string SubKeyPath);

public sealed record InstalledApp
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required AppKind Kind { get; init; }
    public InstallScope Scope { get; init; }

    public string? Publisher { get; init; }
    public string? Version { get; init; }
    public long? SizeBytes { get; init; }
    public DateTime? InstallDate { get; init; }
    public string? InstallLocation { get; init; }

    /// <summary>File holding the icon (exe, dll, ico or png).</summary>
    public string? IconPath { get; init; }
    /// <summary>Icon index (or negative resource id) inside <see cref="IconPath"/>.</summary>
    public int IconIndex { get; init; }

    public string? UninstallCommand { get; init; }
    public string? QuietUninstallCommand { get; init; }
    /// <summary>MSI product code, e.g. {01234567-89AB-CDEF-0123-456789ABCDEF}.</summary>
    public string? ProductCode { get; init; }
    /// <summary>MSIX package full name.</summary>
    public string? PackageFullName { get; init; }
    public RegistryLocation? Registry { get; init; }

    /// <summary>The publisher marked this entry as not removable (NoRemove=1).</summary>
    public bool NoRemove { get; init; }

    public bool CanUninstall => !NoRemove && (Kind switch
    {
        AppKind.Msix => PackageFullName is not null,
        AppKind.Msi => ProductCode is not null,
        _ => UninstallCommand is not null || QuietUninstallCommand is not null,
    });

    public bool SupportsQuietUninstall => Kind is AppKind.Msi or AppKind.Msix || QuietUninstallCommand is not null;
}
