using Purgely.Core.Models;

namespace Purgely.Core.Discovery;

public interface IAppSource
{
    /// <summary>Returns the apps this source knows about. Must not throw for single bad entries.</summary>
    IReadOnlyList<InstalledApp> GetApps();

    /// <summary>True if this source handles apps of the given kind.</summary>
    bool Handles(InstalledApp app);

    /// <summary>Checks whether the app is still installed (used to verify an uninstall).</summary>
    bool IsInstalled(InstalledApp app);
}
