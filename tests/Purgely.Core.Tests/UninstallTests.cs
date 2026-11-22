using System.Diagnostics;
using Purgely.Core.Discovery;
using Purgely.Core.Models;
using Purgely.Core.Uninstall;

namespace Purgely.Core.Tests;

public class UninstallCommandBuilderTests
{
    private static InstalledApp App(AppKind kind, string? uninstall = null, string? quiet = null, string? productCode = null) =>
        new() { Id = "x", Name = "X", Kind = kind, UninstallCommand = uninstall, QuietUninstallCommand = quiet, ProductCode = productCode };

    [Fact]
    public void Msi_always_uses_uninstall_switch()
    {
        var app = App(AppKind.Msi, "MsiExec.exe /I{AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE}",
            productCode: "{AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE}");

        Assert.EndsWith("msiexec.exe\" /x {AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE}", UninstallCommandBuilder.Build(app, quiet: false));
        Assert.EndsWith("/x {AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE} /qn /norestart", UninstallCommandBuilder.Build(app, quiet: true));
    }

    [Fact]
    public void Win32_prefers_quiet_command_only_when_asked()
    {
        var app = App(AppKind.Win32, "u.exe", "u.exe /S");
        Assert.Equal("u.exe", UninstallCommandBuilder.Build(app, quiet: false));
        Assert.Equal("u.exe /S", UninstallCommandBuilder.Build(app, quiet: true));
    }

    [Fact]
    public void Win32_falls_back_to_normal_command_when_no_quiet_one()
    {
        Assert.Equal("u.exe", UninstallCommandBuilder.Build(App(AppKind.Win32, "u.exe"), quiet: true));
    }
}

public class AppCatalogTests
{
    [Fact]
    public void Merge_removes_duplicates_and_sorts_by_name()
    {
        var apps = new[]
        {
            new InstalledApp { Id = "1", Name = "Zeta", Kind = AppKind.Win32, Version = "1", UninstallCommand = "u" },
            new InstalledApp { Id = "2", Name = "alpha", Kind = AppKind.Win32, Version = "1" },
            new InstalledApp { Id = "3", Name = "Alpha", Kind = AppKind.Win32, Version = "1", UninstallCommand = "u" },
        };

        var merged = AppCatalog.Merge(apps);

        Assert.Equal(["3", "1"], merged.Select(a => a.Id));
    }
}

public class JobProcessRunnerTests
{
    private static readonly string Cmd = Path.Combine(Environment.SystemDirectory, "cmd.exe");

    [Fact]
    public async Task Returns_exit_code()
    {
        Assert.Equal(7, await JobProcessRunner.RunAsync($"\"{Cmd}\" /c exit 7"));
    }

    [Fact]
    public async Task Waits_for_child_processes_after_parent_exits()
    {
        // The parent cmd exits at once; the child ping keeps running ~2 seconds.
        var sw = Stopwatch.StartNew();
        await JobProcessRunner.RunAsync($"\"{Cmd}\" /c start \"\" /b ping -n 3 127.0.0.1");
        Assert.True(sw.Elapsed > TimeSpan.FromSeconds(1.5), $"Returned after {sw.Elapsed}");
    }

    [Fact]
    public async Task Cancellation_stops_waiting()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            JobProcessRunner.RunAsync($"\"{Cmd}\" /c ping -n 5 127.0.0.1", cts.Token));
    }
}
