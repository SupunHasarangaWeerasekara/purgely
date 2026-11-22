using System.Windows;

namespace Purgely.App.Services;

public interface IDialogService
{
    bool Confirm(string title, string message);
    void Info(string title, string message);
    void Error(string title, string message);
}

public sealed class MessageBoxDialogService : IDialogService
{
    private static Window? Owner => Application.Current?.MainWindow;

    public bool Confirm(string title, string message) =>
        Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;

    public void Info(string title, string message) =>
        Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);

    public void Error(string title, string message) =>
        Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);

    private static MessageBoxResult Show(string message, string title, MessageBoxButton buttons, MessageBoxImage icon) =>
        Owner is { } owner
            ? MessageBox.Show(owner, message, title, buttons, icon)
            : MessageBox.Show(message, title, buttons, icon);
}
