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

```powershell
dotnet publish src/Purgely.App -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o publish
& "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe" installer\Purgely.iss   # output: dist\
```
