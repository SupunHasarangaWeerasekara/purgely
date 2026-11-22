using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Purgely.App.Services;
using Purgely.Core.Discovery;
using Purgely.Core.Uninstall;

namespace Purgely.App.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly AppCatalog _catalog;
    private readonly UninstallService _uninstaller;
    private readonly IDialogService _dialogs;
    private readonly ThemeService _themes;
    private readonly SettingsStore _settings;
    private CancellationTokenSource? _waitCts;

    public MainViewModel(AppCatalog catalog, UninstallService uninstaller, IDialogService dialogs,
        ThemeService themes, SettingsStore settings)
    {
        _catalog = catalog;
        _uninstaller = uninstaller;
        _dialogs = dialogs;
        _themes = themes;
        _settings = settings;
        _selectedTheme = themes.Theme;
        AppsView = CollectionViewSource.GetDefaultView(Apps);
        AppsView.Filter = Matches;
        AppsView.SortDescriptions.Add(new SortDescription(nameof(AppItemViewModel.Name), ListSortDirection.Ascending));
    }

    public ObservableCollection<AppItemViewModel> Apps { get; } = [];
    public ICollectionView AppsView { get; }

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UninstallCommand))]
    private AppItemViewModel? _selectedApp;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UninstallCommand), nameof(RefreshCommand))]
    private bool _isBusy;

    [ObservableProperty]
    private string _busyText = string.Empty;

    [ObservableProperty]
    private bool _canStopWaiting;

    [ObservableProperty]
    private bool _preferQuiet;

    [ObservableProperty]
    private string _summary = string.Empty;

    [ObservableProperty]
    private AppTheme _selectedTheme;

    partial void OnSelectedThemeChanged(AppTheme value)
    {
        _themes.Apply(value);
        _settings.Save(_settings.Load() with { Theme = value });
    }

    partial void OnSearchTextChanged(string value)
    {
        AppsView.Refresh();
        UpdateSummary();
    }

    [RelayCommand(CanExecute = nameof(CanRefresh))]
    public async Task RefreshAsync()
    {
        IsBusy = true;
        BusyText = "Looking for installed apps…";
        try
        {
            var selectedId = SelectedApp?.Model.Id;
            var apps = await _catalog.GetAppsAsync();

            Apps.Clear();
            foreach (var app in apps) Apps.Add(new AppItemViewModel(app));
            SelectedApp = Apps.FirstOrDefault(a => a.Model.Id == selectedId);
            UpdateSummary();

            _ = LoadIconsAsync(Apps.ToList());
        }
        catch (Exception ex)
        {
            _dialogs.Error("Purgely", $"Could not read the installed apps.\n\n{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanRefresh() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanUninstall))]
    private async Task UninstallAsync()
    {
        if (SelectedApp is not { } item) return;
        var app = item.Model;
        var quiet = PreferQuiet && app.SupportsQuietUninstall;

        var how = quiet ? "It will be removed silently, without showing its uninstaller." : "Its uninstaller will open - follow its steps.";
        if (!_dialogs.Confirm("Uninstall app", $"Uninstall {app.Name}?\n\n{how}")) return;

        _waitCts = new CancellationTokenSource();
        IsBusy = true;
        CanStopWaiting = true;
        BusyText = $"Uninstalling {app.Name}…\nWaiting for the uninstaller to finish.";
        try
        {
            var result = await _uninstaller.UninstallAsync(app, quiet, _waitCts.Token);
            CanStopWaiting = false;

            switch (result.Outcome)
            {
                case UninstallOutcome.Removed:
                    Apps.Remove(item);
                    UpdateSummary();
                    _dialogs.Info("Uninstalled", result.Message);
                    break;
                case UninstallOutcome.Failed:
                    _dialogs.Error("Uninstall failed", result.Message);
                    break;
                default:
                    _dialogs.Info("Uninstall", result.Message);
                    break;
            }
        }
        catch (Exception ex)
        {
            _dialogs.Error("Uninstall failed", ex.Message);
        }
        finally
        {
            CanStopWaiting = false;
            IsBusy = false;
            _waitCts.Dispose();
            _waitCts = null;
        }

        await RefreshAsync();
    }

    private bool CanUninstall() => !IsBusy && SelectedApp?.Model.CanUninstall == true;

    [RelayCommand]
    private void StopWaiting() => _waitCts?.Cancel();

    private bool Matches(object obj)
    {
        if (string.IsNullOrWhiteSpace(SearchText)) return true;
        var app = (AppItemViewModel)obj;
        var q = SearchText.Trim();
        return app.Name.Contains(q, StringComparison.CurrentCultureIgnoreCase) ||
               app.Publisher.Contains(q, StringComparison.CurrentCultureIgnoreCase);
    }

    private void UpdateSummary()
    {
        var shown = AppsView.Cast<object>().Count();
        Summary = shown == Apps.Count ? $"{Apps.Count} apps installed" : $"{shown} of {Apps.Count} apps";
    }

    private static async Task LoadIconsAsync(IReadOnlyList<AppItemViewModel> items)
    {
        foreach (var item in items)
        {
            var icon = await Task.Run(() => IconLoader.Load(item.Model));
            if (icon is not null) item.Icon = icon;
        }
    }
}
