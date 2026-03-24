using AISEP.Domain.Interfaces;

namespace AISEP.Infrastructure.Services;

/// <summary>
/// Local file-system storage for development.
/// Files are saved under {BasePath}/{folder}/{guid}_{filename}.
/// Replace with Azure Blob / S3 implementation for production.
/// </summary>
public class LocalStorageService : IStorageService
{
    private readonly string _basePath;

    private static readonly Dictionary<string, string> MimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"]  = "application/pdf",
        [".doc"]  = "application/msword",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".ppt"]  = "application/vnd.ms-powerpoint",
        [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        [".xls"]  = "application/vnd.ms-excel",
        [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        [".png"]  = "image/png",
        [".jpg"]  = "image/jpeg",
        [".jpeg"] = "image/jpeg",
    };

    public LocalStorageService(string basePath)
    {
        _basePath = Path.GetFullPath(basePath);
    }

    private string ResolveSafePath(string pathOrKey)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_basePath, pathOrKey));
        if (!fullPath.StartsWith(_basePath, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Invalid file path");
        return fullPath;
    }

    public async Task<StoredFileResult> SaveAsync(Stream stream, string fileName, string folder, CancellationToken ct = default)
    {
        var safeFileName = SanitizeFileName(fileName);
        var uniqueName = $"{Guid.NewGuid():N}_{safeFileName}";
        var relativePath = Path.Combine(folder, uniqueName).Replace('\\', '/');
        var fullPath = ResolveSafePath(relativePath);

        var directory = Path.GetDirectoryName(fullPath)!;
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        await using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);
        await stream.CopyToAsync(fileStream, ct);

        var contentType = GetContentType(safeFileName);

        return new StoredFileResult
        {
            Key = relativePath,
            Url = null, // Local storage has no public URL
            Size = fileStream.Length,
            ContentType = contentType,
            OriginalFileName = fileName
        };
    }

    public Task<Stream> OpenReadAsync(string pathOrKey, CancellationToken ct = default)
    {
        var fullPath = ResolveSafePath(pathOrKey);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"File not found: {pathOrKey}");

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string pathOrKey, CancellationToken ct = default)
    {
        var fullPath = ResolveSafePath(pathOrKey);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string pathOrKey, CancellationToken ct = default)
    {
        var fullPath = ResolveSafePath(pathOrKey);
        return Task.FromResult(File.Exists(fullPath));
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
        return sanitized.Length > 100 ? sanitized[..100] : sanitized;
    }

    private static string GetContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName);
        if (!string.IsNullOrEmpty(ext) && MimeTypes.TryGetValue(ext, out var mime))
            return mime;
        return "application/octet-stream";
    }
}
