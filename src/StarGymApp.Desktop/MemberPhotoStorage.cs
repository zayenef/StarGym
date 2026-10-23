using System.IO;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using StarGymApp.Core.Models;

namespace StarGymApp.Desktop;

internal static class MemberPhotoStorage
{
    private static string PhotoDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "StarGym",
        "Images",
        "Members");

    public static string Save(byte[] photo)
    {
        Directory.CreateDirectory(PhotoDirectory);
        var path = Path.Combine(PhotoDirectory, $"{Guid.NewGuid():N}.jpg");
        File.WriteAllBytes(path, photo);
        return path;
    }

    public static string? ResolvePath(string? photoPath)
    {
        if (string.IsNullOrWhiteSpace(photoPath)) return null;
        var path = Path.IsPathRooted(photoPath)
            ? photoPath
            : Path.Combine(AppContext.BaseDirectory, photoPath);
        if (File.Exists(path)) return path;

        if (!Path.IsPathRooted(photoPath))
        {
            var localPath = Path.Combine(PhotoDirectory, photoPath);
            if (File.Exists(localPath)) return localPath;
        }

        return null;
    }

    public static void Delete(string path)
    {
        if (File.Exists(path)) File.Delete(path);
    }

    public static void ClearAllStoredPhotos()
    {
        if (!Directory.Exists(PhotoDirectory)) return;
        foreach (var photoPath in Directory.EnumerateFiles(PhotoDirectory))
        {
            File.Delete(photoPath);
        }
    }

    public static ImageSource LoadImage(Member member)
    {
        var path = ResolvePath(member.PhotoPath);
        if (path is not null)
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(path, UriKind.Absolute);
            image.EndInit();
            image.Freeze();
            return image;
        }

        var initials = string.Concat(member.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(2)
            .Select(part => part[0]))
            .ToUpperInvariant();
        var visual = new DrawingVisual();
        using (var drawing = visual.RenderOpen())
        {
            drawing.DrawRectangle(new SolidColorBrush(Color.FromRgb(23, 105, 224)), null, new Rect(0, 0, 120, 120));
            var text = new FormattedText(initials, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface("Segoe UI"), 38, Brushes.White, 1.0);
            drawing.DrawText(text, new Point((120 - text.Width) / 2, (120 - text.Height) / 2));
        }

        var bitmap = new RenderTargetBitmap(120, 120, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }
}

public sealed class MemberPhotoConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is Member member ? MemberPhotoStorage.LoadImage(member) : DependencyProperty.UnsetValue;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
