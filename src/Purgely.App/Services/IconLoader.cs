using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Purgely.Core.Models;

namespace Purgely.App.Services;

/// <summary>Loads app icons off the UI thread. Returned images are frozen so they can cross threads.</summary>
public static class IconLoader
{
    public static ImageSource? Load(InstalledApp app)
    {
        try
        {
            if (app.IconPath is not { } path || !File.Exists(path)) return null;

            var ext = Path.GetExtension(path).ToLowerInvariant();
            return ext is ".png" or ".jpg" or ".jpeg" or ".bmp"
                ? LoadImageFile(path)
                : ExtractIcon(path, app.IconIndex);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static ImageSource LoadImageFile(string path)
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad; // don't keep the file locked
        image.DecodePixelWidth = 64;
        image.UriSource = new Uri(path);
        image.EndInit();
        image.Freeze();
        return image;
    }

    private static ImageSource? ExtractIcon(string path, int index)
    {
        var large = new IntPtr[1];
        if (ExtractIconExW(path, index, large, null, 1) == 0 || large[0] == IntPtr.Zero) return null;

        try
        {
            var source = Imaging.CreateBitmapSourceFromHIcon(large[0], Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            source.Freeze();
            return source;
        }
        finally
        {
            DestroyIcon(large[0]);
        }
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern uint ExtractIconExW(string file, int index, IntPtr[]? large, IntPtr[]? small, uint count);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr handle);
}
