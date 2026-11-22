using System.ComponentModel;
using Purgely.Core.Discovery;
using Purgely.Core.Models;
using Windows.Management.Deployment;

namespace Purgely.Core.Uninstall;

public enum UninstallOutcome
{
    Removed,
    StillInstalled,
    Cancelled,
    Failed,
}

public sealed record UninstallResult(UninstallOutcome Outcome, string Message, int? ExitCode = null, bool RebootRequired = false);

public sealed class UninstallService(AppCatalog catalog)
{
    // Windows Installer exit codes
    private const int MsiUserCancelled = 1602;
    private const int MsiUnknownProduct = 1605;
    private const int MsiRebootInitiated = 1641;
    private const int MsiRebootRequired = 3010;

    /// <summary>
    /// Uninstalls the app and verifies the result by checking whether it is still registered.
    /// Cancelling <paramref name="ct"/> stops waiting; the uninstaller itself keeps running.
    /// </summary>
    public async Task<UninstallResult> UninstallAsync(InstalledApp app, bool quiet, CancellationToken ct = default)
    {
        if (!app.CanUninstall)
            return new(UninstallOutcome.Failed, $"{app.Name} cannot be uninstalled (the publisher marked it as not removable).");

        try
        {
            return app.Kind == AppKind.Msix
                ? await RemovePackageAsync(app, ct)
                : await RunUninstallerAsync(app, quiet, ct);
        }
        catch (OperationCanceledException)
        {
            return new(UninstallOutcome.Cancelled,
                "Stopped waiting. The uninstaller may still be running - refresh the list when it is done.");
        }
        catch (Win32Exception ex)
        {
            return new(UninstallOutcome.Failed, $"Could not start the uninstaller: {ex.Message}");
        }
    }

    private async Task<UninstallResult> RunUninstallerAsync(InstalledApp app, bool quiet, CancellationToken ct)
    {
        var command = UninstallCommandBuilder.Build(app, quiet);
        var exitCode = await JobProcessRunner.RunAsync(command, ct);
        var reboot = exitCode is MsiRebootRequired or MsiRebootInitiated;

        if (!catalog.IsInstalled(app) || exitCode == MsiUnknownProduct)
        {
            var msg = reboot
                ? $"{app.Name} was removed. Restart Windows to finish."
                : $"{app.Name} was removed.";
            return new(UninstallOutcome.Removed, msg, exitCode, reboot);
        }

        if (exitCode == MsiUserCancelled)
            return new(UninstallOutcome.Cancelled, "The uninstall was cancelled.", exitCode);

        var detail = reboot
            ? " It may be removed after you restart Windows."
            : exitCode is int c and not 0 ? $" (exit code {c})" : string.Empty;
        return new(UninstallOutcome.StillInstalled, $"{app.Name} is still installed.{detail}", exitCode, reboot);
    }

    private static async Task<UninstallResult> RemovePackageAsync(InstalledApp app, CancellationToken ct)
    {
        var result = await new PackageManager().RemovePackageAsync(app.PackageFullName).AsTask(ct);
        return result.ExtendedErrorCode is null
            ? new(UninstallOutcome.Removed, $"{app.Name} was removed.")
            : new(UninstallOutcome.Failed, $"Could not remove {app.Name}: {result.ErrorText}");
    }
}
