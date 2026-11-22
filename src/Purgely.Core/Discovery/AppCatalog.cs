using Purgely.Core.Models;

namespace Purgely.Core.Discovery;

/// <summary>Combines all app sources into one de-duplicated, name-sorted list.</summary>
public sealed class AppCatalog(IEnumerable<IAppSource> sources)
{
    private readonly IReadOnlyList<IAppSource> _sources = sources.ToList();

    public static AppCatalog CreateDefault() => new([new RegistryAppSource(), new MsixAppSource()]);

    public Task<IReadOnlyList<InstalledApp>> GetAppsAsync(CancellationToken ct = default) =>
        Task.Run(() => Merge(_sources.SelectMany(s => s.GetApps())), ct);

    public bool IsInstalled(InstalledApp app) =>
        _sources.FirstOrDefault(s => s.Handles(app))?.IsInstalled(app) ?? false;

    /// <summary>
    /// The same program can be registered more than once (e.g. under both the 64-bit and
    /// 32-bit registry views). Keep the first entry per name + version.
    /// </summary>
    internal static IReadOnlyList<InstalledApp> Merge(IEnumerable<InstalledApp> apps) =>
        apps
            .GroupBy(a => (Name: a.Name.ToUpperInvariant(), Version: a.Version?.ToUpperInvariant()))
            .Select(g => g.OrderByDescending(a => a.CanUninstall).ThenByDescending(a => a.SupportsQuietUninstall).First())
            .OrderBy(a => a.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
}
