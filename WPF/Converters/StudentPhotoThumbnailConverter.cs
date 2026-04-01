using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using WPF.Services;

namespace WPF.Converters;

public sealed class StudentPhotoThumbnailConverter : IValueConverter
{
    private static readonly Dictionary<string, BitmapImage> Cache = new(StringComparer.OrdinalIgnoreCase);

    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string storedPhotoPath || string.IsNullOrWhiteSpace(storedPhotoPath))
        {
            return null;
        }

        var resolvedPath = StudentPhotoStorage.ResolvePhotoPath(storedPhotoPath);
        if (string.IsNullOrWhiteSpace(resolvedPath) || !File.Exists(resolvedPath))
        {
            return null;
        }

        if (Cache.TryGetValue(resolvedPath, out var cachedImage))
        {
            return cachedImage;
        }

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.UriSource = new Uri(resolvedPath, UriKind.Absolute);
        bitmap.DecodePixelWidth = 72;
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
        bitmap.EndInit();
        bitmap.Freeze();

        Cache[resolvedPath] = bitmap;
        return bitmap;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
