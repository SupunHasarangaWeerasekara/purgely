using System.Windows;
using Purgely.App.Services;
using Purgely.App.ViewModels;
using Purgely.Core.Discovery;
using Purgely.Core.Uninstall;

namespace Purgely.App;

public partial class App : Application
{
    private ThemeService? _themes;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var settings = new SettingsStore();
        _themes = new ThemeService();
        _themes.Apply(settings.Load().Theme); // before any window is created, so there is no flash

        var catalog = AppCatalog.CreateDefault();
        var viewModel = new MainViewModel(catalog, new UninstallService(catalog), new MessageBoxDialogService(),
            _themes, settings);

        var window = new MainWindow { DataContext = viewModel };
        window.SourceInitialized += (_, _) => _themes.ApplyTitleBar(window);
        MainWindow = window;
        window.Show();

        _ = viewModel.RefreshAsync();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _themes?.Dispose();
        base.OnExit(e);
    }
}
