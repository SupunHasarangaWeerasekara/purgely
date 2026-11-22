using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Purgely.Core.Models;

namespace Purgely.App.ViewModels;

public sealed partial class AppItemViewModel(InstalledApp app) : ObservableObject
{
    public InstalledApp Model { get; } = app;

    [ObservableProperty]
    private ImageSource? _icon;

    public string Name => Model.Name;
    public string Publisher => Model.Publisher ?? string.Empty;
    public string Version => Model.Version ?? string.Empty;

    // Sort keys (DataGrid SortMemberPath). Unknown values sort last.
    public long SizeSortKey => Model.SizeBytes ?? -1;
    public DateTime DateSortKey => Model.InstallDate ?? DateTime.MinValue;

    public string SizeText => Model.SizeBytes is long b ? FormatSize(b) : string.Empty;
    public string InstallDateText => Model.InstallDate?.ToString("yyyy-MM-dd") ?? string.Empty;

    public string KindText => Model.Kind switch
    {
        AppKind.Msix => "Store app",
        AppKind.Msi => "Windows Installer",
        _ => "Desktop app",
    };

    public string ScopeText => Model.Scope == InstallScope.User ? "Current user" : "All users";

    public bool SupportsQuietUninstall => Model.SupportsQuietUninstall;

    public string Initial => Name.Length > 0 ? char.ToUpperInvariant(Name[0]).ToString() : "?";

    internal static string FormatSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double value = bytes;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }
        return unit == 0 ? $"{bytes} B" : $"{value:0.#} {units[unit]}";
    }
}
