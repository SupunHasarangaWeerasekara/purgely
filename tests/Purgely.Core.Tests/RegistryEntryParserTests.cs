using Microsoft.Win32;
using Purgely.Core.Discovery;
using Purgely.Core.Models;

namespace Purgely.Core.Tests;

public class RegistryEntryParserTests
{
    private static readonly RegistryLocation Hklm64 =
        new(RegistryHive.LocalMachine, RegistryView.Registry64, @"SOFTWARE\...\Uninstall\X");

    private static InstalledApp? Parse(string key, params (string Name, object? Value)[] values) =>
        RegistryEntryParser.TryParse(key, values.ToDictionary(v => v.Name, v => v.Value), Hklm64);

    [Fact]
    public void Parses_win32_entry()
    {
        var app = Parse("7-Zip",
            ("DisplayName", "7-Zip 24.08 (x64)"),
            ("DisplayVersion", "24.08"),
            ("Publisher", "Igor Pavlov"),
            ("UninstallString", "\"C:\\Program Files\\7-Zip\\Uninstall.exe\""),
            ("EstimatedSize", 5600),
            ("InstallDate", "20260115"),
            ("DisplayIcon", "\"C:\\Program Files\\7-Zip\\7zFM.exe\",0"));

        Assert.NotNull(app);
        Assert.Equal(AppKind.Win32, app.Kind);
        Assert.Equal(InstallScope.Machine, app.Scope);
        Assert.Equal(5600L * 1024, app.SizeBytes);
        Assert.Equal(new DateTime(2026, 1, 15), app.InstallDate);
        Assert.Equal(@"C:\Program Files\7-Zip\7zFM.exe", app.IconPath);
        Assert.True(app.CanUninstall);
        Assert.False(app.SupportsQuietUninstall);
    }

    [Fact]
    public void Detects_msi_from_WindowsInstaller_flag()
    {
        var app = Parse("{12345678-1234-1234-1234-123456789abc}",
            ("DisplayName", "Some MSI App"),
            ("WindowsInstaller", 1),
            ("UninstallString", "MsiExec.exe /I{12345678-1234-1234-1234-123456789abc}"));

        Assert.NotNull(app);
        Assert.Equal(AppKind.Msi, app.Kind);
        Assert.Equal("{12345678-1234-1234-1234-123456789ABC}", app.ProductCode);
        Assert.True(app.SupportsQuietUninstall);
    }

    [Fact]
    public void Detects_msi_from_uninstall_string()
    {
        var app = Parse("SomeApp",
            ("DisplayName", "Some App"),
            ("UninstallString", "\"C:\\Windows\\System32\\msiexec.exe\" /X {AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE}"));

        Assert.Equal("{AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE}", app?.ProductCode);
    }

    [Theory]
    [InlineData("SystemComponent", 1)]
    [InlineData("ParentKeyName", "OperatingSystem")]
    [InlineData("ReleaseType", "Security Update")]
    [InlineData("ReleaseType", "Hotfix")]
    public void Skips_hidden_and_update_entries(string valueName, object value)
    {
        Assert.Null(Parse("X", ("DisplayName", "Hidden"), ("UninstallString", "u.exe"), (valueName, value)));
    }

    [Fact]
    public void Skips_entries_without_name_or_uninstaller()
    {
        Assert.Null(Parse("X", ("UninstallString", "u.exe")));
        Assert.Null(Parse("X", ("DisplayName", "No uninstaller")));
    }

    [Fact]
    public void NoRemove_entries_are_listed_but_not_uninstallable()
    {
        var app = Parse("X", ("DisplayName", "Locked"), ("UninstallString", "u.exe"), ("NoRemove", 1));
        Assert.NotNull(app);
        Assert.False(app.CanUninstall);
    }

    [Theory]
    [InlineData("C:\\App\\app.exe,0", "C:\\App\\app.exe", 0)]
    [InlineData("\"C:\\App\\app.exe\",-101", "C:\\App\\app.exe", -101)]
    [InlineData("C:\\App\\app.ico", "C:\\App\\app.ico", 0)]
    [InlineData("C:\\My,Folder\\app.exe", "C:\\My,Folder\\app.exe", 0)]
    public void Parses_display_icon(string raw, string path, int index)
    {
        Assert.Equal((path, index), RegistryEntryParser.ParseIcon(raw));
    }
}
