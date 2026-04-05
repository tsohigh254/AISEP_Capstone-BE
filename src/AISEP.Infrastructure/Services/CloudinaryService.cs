using AISEP.Application.Configuration;
using AISEP.Application.DTOs.Common;
using AISEP.Application.Interfaces;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace AISEP.Infrastructure.Services
{
    public class CloudinaryService : ICloudinaryService
    {
        private static readonly string[] AllowedExtensionsImage = { ".jpeg", ".gif", ".png", ".jpg" };
        private static readonly string[] AllowedExtensionsDocument = { ".pdf", ".ppt", ".pptx", ".doc", ".docx" };

        private readonly Cloudinary _cloudinary;
        private readonly CloudinaryOptions _options;

        private const int MaxFileSizeImage = 5 * 1024 * 1024;
        private const int MaxFileSizeDocument = 20 * 1024 * 1024;

        public CloudinaryService(IOptions<CloudinaryOptions> options)
        {
            _options = options.Value;

            var account = new Account(
                _options.CloudName,
                _options.ApiKey,
                _options.ApiSecret
            );

            _cloudinary = new Cloudinary(account);
        }

        public async Task DeleteImage(string url)
        {
            var publicId = ExtractImagePublicIdFromUrl(url);

            if (publicId == null)
            {
                throw new InvalidOperationException($"Failed to extract publicId from URL: {url}");
            }

            var deleteParams = new DeletionParams(publicId);
            await _cloudinary.DestroyAsync(deleteParams);
        }

        public async Task<string> UploadImage(IFormFile file, string folder)
        {
            if (file == null || file.Length == 0)
            {
                throw new FileNotFoundException("Image file cannot be empty.");
            }

            if (file.Length > MaxFileSizeImage)
            {
                throw new InvalidOperationException($"Image must not exceed {MaxFileSizeImage / (1024 * 1024)} MB.");
            }

            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensionsImage.Contains(fileExtension))
            {
                throw new ArgumentException($"Only these image extensions are allowed: {string.Join(",", AllowedExtensionsImage)}");
            }

            using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folder
            };

            var result = await _cloudinary.UploadAsync(uploadParams);
            return result.SecureUrl?.ToString()
                ?? throw new InvalidOperationException("Image upload failed: secure URL was not returned.");
        }

        public async Task<string> UploadDocument(IFormFile file, string folder)
        {
            var result = await UploadDocumentWithMetadata(file, folder);
            return result.Url;
        }

        public async Task<CloudinaryUploadResultDto> UploadDocumentWithMetadata(IFormFile file, string folder)
        {
            if (file == null || file.Length == 0)
            {
                throw new FileNotFoundException("Document file cannot be empty.");
            }

            if (file.Length > MaxFileSizeDocument)
            {
                throw new InvalidOperationException($"Document must not exceed {MaxFileSizeDocument / (1024 * 1024)} MB.");
            }

            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensionsDocument.Contains(fileExtension))
            {
                throw new ArgumentException($"Only these document extensions are allowed: {string.Join(",", AllowedExtensionsDocument)}");
            }

            using var stream = file.OpenReadStream();

            var uploadParams = new RawUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folder,
                UseFilename = false,
                UniqueFilename = true
            };

            var result = await _cloudinary.UploadAsync(uploadParams);
            if (result?.SecureUrl == null || string.IsNullOrWhiteSpace(result.PublicId))
            {
                throw new InvalidOperationException("Document upload failed: Cloudinary did not return secure URL/public ID.");
            }

            return new CloudinaryUploadResultDto
            {
                Url = result.SecureUrl.ToString(),
                PublicId = result.PublicId
            };
        }

        public string GenerateSignedDocumentUrl(
            string? storageKey,
            string? fallbackUrl = null,
            string? fileName = null,
            int? expiresInMinutes = null)
        {
            var publicId = !string.IsNullOrWhiteSpace(storageKey)
                ? storageKey
                : ExtractDocumentStorageKeyFromUrl(fallbackUrl);

            if (string.IsNullOrWhiteSpace(publicId))
            {
                return fallbackUrl ?? string.Empty;
            }

            var expiresAt = DateTimeOffset.UtcNow
                .AddMinutes(expiresInMinutes ?? _options.SignedUrlExpirationMinutes)
                .ToUnixTimeSeconds();

            return _cloudinary.DownloadPrivate(
                publicId,
                false,
                null,
                "upload",
                expiresAt,
                "raw");
        }

        public string? ExtractDocumentStorageKeyFromUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return null;
            }

            var path = uri.AbsolutePath;
            var parts = path.Split("/upload/");
            if (parts.Length < 2)
            {
                return null;
            }

            var pathAfterUpload = parts[1];
            var segments = pathAfterUpload
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            if (segments.Count == 0)
            {
                return null;
            }

            if (segments[0].StartsWith("v", StringComparison.OrdinalIgnoreCase) && segments[0].Length > 1)
            {
                segments.RemoveAt(0);
            }

            return segments.Count == 0 ? null : string.Join("/", segments);
        }

        private static string? ExtractImagePublicIdFromUrl(string imageUrl)
        {
            if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri))
            {
                return null;
            }

            var path = uri.AbsolutePath;
            var parts = path.Split("/upload/");
            if (parts.Length < 2)
            {
                throw new ArgumentException("Invalid Cloudinary image URL.");
            }

            var pathAfterUpload = parts[1];
            var segments = pathAfterUpload.Split('/').ToList();

            if (segments.Count > 0 && segments[0].StartsWith("v", StringComparison.OrdinalIgnoreCase) && segments[0].Length > 1)
            {
                segments.RemoveAt(0);
            }

            var fullPath = string.Join("/", segments);
            return Path.ChangeExtension(fullPath, null);
        }
    }
}
