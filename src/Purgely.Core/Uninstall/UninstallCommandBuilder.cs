using Purgely.Core.Models;

namespace Purgely.Core.Uninstall;

public static class UninstallCommandBuilder
{
    /// <summary>Builds the command line that removes a Win32 or MSI app.</summary>
    public static string Build(InstalledApp app, bool quiet)
    {
        if (app.Kind == AppKind.Msix)
            throw new ArgumentException("MSIX apps are removed with PackageManager, not a command line.", nameof(app));

        if (app.ProductCode is not null)
        {
            // Always /x: many MSI entries register "MsiExec.exe /I{GUID}", which opens the
            // repair/modify dialog instead of uninstalling.
            var msiexec = Path.Combine(Environment.SystemDirectory, "msiexec.exe");
            var args = quiet ? " /qn /norestart" : string.Empty;
            return $"\"{msiexec}\" /x {app.ProductCode}{args}";
        }

        if (quiet && app.QuietUninstallCommand is not null) return app.QuietUninstallCommand;

        return app.UninstallCommand ?? app.QuietUninstallCommand
            ?? throw new InvalidOperationException($"{app.Name} has no uninstall command.");
    }
}
