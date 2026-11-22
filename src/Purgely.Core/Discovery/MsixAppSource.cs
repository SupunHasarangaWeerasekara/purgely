using Purgely.Core.Models;
using Windows.ApplicationModel;
using Windows.Management.Deployment;

namespace Purgely.Core.Discovery;

/// <summary>Reads Store / MSIX / UWP apps installed for the current user.</summary>
public sealed class MsixAppSource : IAppSource
{
    public IReadOnlyList<InstalledApp> GetApps()
    {
        var apps = new List<InstalledApp>();
        IEnumerable<Package> packages;
        try
        {
            // Empty SID = current user.
            packages = new PackageManager().FindPackagesForUser(string.Empty).ToList();
        }
        catch (Exception)
        {
            return apps;
        }

        foreach (var package in packages)
        {
            try
            {
                var app = ToApp(package);
                if (app is not null) apps.Add(app);
            }
            catch (Exception)
            {
                // Some packages throw for individual properties (e.g. staged/broken installs). Skip them.
            }
        }

        return apps;
    }

    public bool Handles(InstalledApp app) => app.Kind == AppKind.Msix;

    public bool IsInstalled(InstalledApp app) =>
        app.PackageFullName is not null &&
        new PackageManager().FindPackageForUser(string.Empty, app.PackageFullName) is not null;

    private static InstalledApp? ToApp(Package package)
    {
        // Frameworks, resource packs and inbox system apps are not user-removable programs.
        if (package.IsFramework || package.IsResourcePackage) return null;
        if (package.SignatureKind == PackageSignatureKind.System) return null;
        // Packages without a Start menu entry are extensions/services, not apps the user knows.
        if (package.GetAppListEntries().Count == 0) return null;

        var name = package.DisplayName;
        if (string.IsNullOrWhiteSpace(name) || name.StartsWith("ms-resource:", StringComparison.OrdinalIgnoreCase))
            name = package.Id.Name;

        var v = package.Id.Version;
        string? location = null;
        try { location = package.InstalledPath; } catch (Exception) { }

        return new InstalledApp
        {
            Id = $"msix:{package.Id.FullName}",
            Name = name,
            Kind = AppKind.Msix,
            Scope = InstallScope.User,
            Publisher = NullIfEmpty(package.PublisherDisplayName),
            Version = $"{v.Major}.{v.Minor}.{v.Build}.{v.Revision}",
            InstallDate = package.InstalledDate.Year > 1601 ? package.InstalledDate.LocalDateTime : null,
            InstallLocation = location,
            IconPath = ResolveLogo(package),
            PackageFullName = package.Id.FullName,
        };
    }

    /// <summary>
    /// Package.Logo points at the manifest path (e.g. Assets\StoreLogo.png) but the file on disk is
    /// usually a scaled variant (StoreLogo.scale-100.png). Find the best existing file.
    /// </summary>
    private static string? ResolveLogo(Package package)
    {
        try
        {
            var logo = package.Logo?.LocalPath;
            if (logo is null) return null;
            if (File.Exists(logo)) return logo;

            var dir = Path.GetDirectoryName(logo);
            if (dir is null || !Directory.Exists(dir)) return null;

            var pattern = $"{Path.GetFileNameWithoutExtension(logo)}*{Path.GetExtension(logo)}";
            return Directory.EnumerateFiles(dir, pattern)
                .OrderBy(f => f.Contains("scale-100", StringComparison.OrdinalIgnoreCase) ? 0
                    : f.Contains("scale-200", StringComparison.OrdinalIgnoreCase) ? 1 : 2)
                .FirstOrDefault();
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;
}
