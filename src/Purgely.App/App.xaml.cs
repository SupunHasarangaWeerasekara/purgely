using System.Windows;
using Purgely.App.Services;
using Purgely.App.ViewModels;
using Purgely.Core.Discovery;
using Purgely.Core.Uninstall;

namespace Purgely.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var catalog = AppCatalog.CreateDefault();
        var viewModel = new MainViewModel(catalog, new UninstallService(catalog), new MessageBoxDialogService());

        var window = new MainWindow { DataContext = viewModel };
        MainWindow = window;
        window.Show();

        _ = viewModel.RefreshAsync();
    }
}
