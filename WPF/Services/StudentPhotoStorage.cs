using System;
using System.IO;
using System.Linq;

namespace WPF.Services;

public static class StudentPhotoStorage
{
    public static string DefaultStorageRoot =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WpfNfcAttendanceManagement",
            "StudentPhotos");

    public static string SavePhoto(string sourceFilePath, string studentCode)
    {
        return SavePhoto(sourceFilePath, DefaultStorageRoot, studentCode);
    }

    public static string SavePhoto(string sourceFilePath, string storageRoot, string studentCode)
    {
        if (string.IsNullOrWhiteSpace(sourceFilePath))
        {
            throw new ArgumentException("Source file path was empty.", nameof(sourceFilePath));
        }

        if (!File.Exists(sourceFilePath))
        {
            throw new FileNotFoundException("Photo source file was not found.", sourceFilePath);
        }

        Directory.CreateDirectory(storageRoot);

        var safeStudentCode = BuildSafeStudentCode(studentCode);
        var extension = string.IsNullOrWhiteSpace(Path.GetExtension(sourceFilePath))
            ? ".img"
            : Path.GetExtension(sourceFilePath).ToLowerInvariant();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var fileName = $"{safeStudentCode}_{DateTime.UtcNow:yyyyMMddHHmmssfff}_{suffix}{extension}";
        var destinationPath = Path.Combine(storageRoot, fileName);

        File.Copy(sourceFilePath, destinationPath, overwrite: true);
        return fileName;
    }

    public static string? ResolvePhotoPath(string? storedPhotoPath)
    {
        return ResolvePhotoPath(storedPhotoPath, DefaultStorageRoot);
    }

    public static string? ResolvePhotoPath(string? storedPhotoPath, string storageRoot)
    {
        if (string.IsNullOrWhiteSpace(storedPhotoPath))
        {
            return null;
        }

        return Path.IsPathRooted(storedPhotoPath)
            ? storedPhotoPath
            : Path.Combine(storageRoot, storedPhotoPath);
    }

    public static void DeletePhoto(string? storedPhotoPath)
    {
        DeletePhoto(storedPhotoPath, DefaultStorageRoot);
    }

    public static void DeletePhoto(string? storedPhotoPath, string storageRoot)
    {
        var resolvedPath = ResolvePhotoPath(storedPhotoPath, storageRoot);
        if (string.IsNullOrWhiteSpace(resolvedPath))
        {
            return;
        }

        var fullStorageRoot = Path.GetFullPath(storageRoot);
        var fullResolvedPath = Path.GetFullPath(resolvedPath);
        if (!fullResolvedPath.StartsWith(fullStorageRoot, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (File.Exists(fullResolvedPath))
        {
            File.Delete(fullResolvedPath);
        }
    }

    private static string BuildSafeStudentCode(string studentCode)
    {
        var safeValue = new string((studentCode ?? string.Empty)
            .Where(char.IsLetterOrDigit)
            .ToArray());

        return string.IsNullOrWhiteSpace(safeValue) ? "student" : safeValue;
    }
}
