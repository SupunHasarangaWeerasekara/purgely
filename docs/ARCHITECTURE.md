# Purgely — Architecture

Purgely is a Windows uninstaller: it lists installed programs, runs their uninstallers,
and then finds and safely removes leftover files and registry entries.

**Stack:** C# / .NET (LTS) / WPF, MVVM via CommunityToolkit.Mvvm. Installer: Inno Setup.

## Where installed apps come from

| Kind | Discovery | Removal |
|---|---|---|
| Win32 (EXE: NSIS, Inno, …) | `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall`, `HKLM\SOFTWARE\WOW6432Node\…\Uninstall`, `HKCU\…\Uninstall` | `UninstallString` / `QuietUninstallString` |
| MSI | Same keys, key name is `{ProductCode}` | `msiexec /x {GUID}` (`/qn` for silent) |
| MSIX / Store | `PackageManager.FindPackagesForUser("")` | `PackageManager.RemovePackageAsync` |

Skip entries with `SystemComponent=1`, a `ParentKeyName` (updates), no `DisplayName`,
and framework / non-removable MSIX packages.

## Layers

```
Purgely.App             WPF UI: app list, search/sort, details, progress, leftover review
Purgely.Core            Models + services
  Discovery/            IAppSource: RegistryAppSource, MsixAppSource
  Uninstall/            IUninstaller: MsiUninstaller, ExeUninstaller, MsixUninstaller
  Leftovers/            LeftoverScanner (files, registry, shortcuts, startup entries)
  Safety/               Restore point, .reg backup, Recycle Bin delete, path blocklist
Purgely.Infrastructure  Registry access, process + Job Objects, icon extraction, logging
tests/Purgely.Core.Tests
```

## Key rules

- **Wait for the whole process tree.** Many uninstallers relaunch themselves from `%TEMP%`
  and exit; run them inside a Job Object and wait until every process in it has exited.
- **Exit codes:** MSI `0` ok, `1605` not installed, `3010` reboot required.
- **Leftovers are never deleted automatically.** Show results labeled *Safe* / *Review*;
  delete only what the user ticks. Files go to the Recycle Bin; registry keys are exported
  to `.reg` first.
- **Blocklist:** never touch `C:\Windows`, `System32`, the root of `Program Files`,
  user profile roots, or shared publisher folders that still contain other installed apps.
- **Elevation:** run `asInvoker`; request UAC only for machine-wide (HKLM) uninstalls.

## Phases

1. **MVP:** list apps (registry + MSIX), search/sort, uninstall one app.
2. **Robust uninstall:** Job Object waiting, silent mode, batch uninstall, logging.
3. **Leftover scanner** with backup + Recycle Bin.
4. **Polish:** dark mode, size calculation, CSV export, optional winget integration,
   install monitoring for exact cleanup.
