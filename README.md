# Purgely

A Windows uninstaller that removes apps — and the junk they leave behind.

![Purgely icon](assets/purgely-256.png)

## Features (so far)

- Lists desktop programs (EXE and MSI) and Microsoft Store apps
- Search by name or publisher; sort by name, size, install date or type
- Uninstalls with the app's own uninstaller, or silently when the app supports it
- Waits for the whole uninstaller process tree (Job Objects), then checks the app is really gone

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for the design and roadmap.

## Requirements

- Windows 10 version 2004 or later / Windows 11
- [.NET 10 SDK](https://dotnet.microsoft.com/download) to build

## Build and run

```powershell
dotnet build Purgely.slnx
dotnet test Purgely.slnx
dotnet run --project src/Purgely.App   # asks for administrator rights
```

## Installer

Needs [Inno Setup 6](https://jrsoftware.org/isinfo.php). Runs the tests, publishes a
self-contained single-file `Purgely.exe` (no .NET install needed on the target PC) and
builds `dist\Purgely-Setup-<version>.exe`:

```powershell
powershell -ExecutionPolicy Bypass -File tools\build-installer.ps1
```

To release a new version, bump `<Version>` in `Directory.Build.props` and `AppVersion` in
`installer\Purgely.iss`.
