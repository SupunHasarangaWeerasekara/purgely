using System.Windows;
using System.Windows.Input;
using Purgely.App.ViewModels;

namespace Purgely.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void AppsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel vm && vm.UninstallCommand.CanExecute(null))
            vm.UninstallCommand.Execute(null);
    }
}
