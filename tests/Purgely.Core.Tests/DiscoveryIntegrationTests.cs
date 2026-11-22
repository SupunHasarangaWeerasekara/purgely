using Purgely.Core.Discovery;
using Purgely.Core.Models;
using Xunit.Abstractions;

namespace Purgely.Core.Tests;

/// <summary>Reads the real registry / package store of the machine running the tests.</summary>
public class DiscoveryIntegrationTests(ITestOutputHelper output)
{
    [Fact]
    public async Task Finds_installed_apps_on_this_machine()
    {
        var apps = await AppCatalog.CreateDefault().GetAppsAsync();

        foreach (var group in apps.GroupBy(a => a.Kind))
            output.WriteLine($"{group.Key}: {group.Count()}");
        foreach (var app in apps.Take(15))
            output.WriteLine($"  [{app.Kind}] {app.Name} | {app.Publisher} | {app.Version}");

        Assert.NotEmpty(apps);
        Assert.Contains(apps, a => a.Kind is AppKind.Win32 or AppKind.Msi);
        Assert.All(apps, a => Assert.True(AppCatalog.CreateDefault().IsInstalled(a), $"{a.Name} should be installed"));
    }
}
