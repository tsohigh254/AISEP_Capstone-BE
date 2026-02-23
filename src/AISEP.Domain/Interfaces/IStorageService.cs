namespace AISEP.Domain.Interfaces;

/// <summary>
/// Abstraction for file storage (local dev, Azure Blob, S3, etc.)
/// </summary>
public interface IStorageService
{
    /// <summary>Save an uploaded file to storage.</summary>
    Task<StoredFileResult> SaveAsync(Stream stream, string fileName, string folder, CancellationToken ct = default);

    /// <summary>Open a readable stream for a stored file.</summary>
    Task<Stream> OpenReadAsync(string pathOrKey, CancellationToken ct = default);

    /// <summary>Delete a stored file.</summary>
    Task DeleteAsync(string pathOrKey, CancellationToken ct = default);

    /// <summary>Check if a file exists in storage.</summary>
    Task<bool> ExistsAsync(string pathOrKey, CancellationToken ct = default);
}

/// <summary>
/// Result returned after saving a file to storage.
/// </summary>
public class StoredFileResult
{
    /// <summary>Relative path or object key used to retrieve the file later.</summary>
    public string Key { get; set; } = null!;

    /// <summary>Public URL if applicable (null for local storage).</summary>
    public string? Url { get; set; }

    /// <summary>File size in bytes.</summary>
    public long Size { get; set; }

    /// <summary>MIME content type.</summary>
    public string ContentType { get; set; } = null!;

    /// <summary>Original file name as uploaded.</summary>
    public string OriginalFileName { get; set; } = null!;
}
