using AISEP.Application.Configuration;
using AISEP.Application.Interfaces;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AISEP.Infrastructure.Services
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly string[] allowedExtensionsImage = { ".jpeg", ".gif", ".png", ".jpg" };
        private readonly string[] allowedExtensionsDocument = { ".pdf", ".ppt", ".pptx", ".doc", ".docx" };
        private readonly Cloudinary _cloudinary;
        private const int MaxFileSizeImage = 5 * 1024 * 1024;
        private const int MaxFileSizeDocument = 20 * 1024 * 1024;


        public CloudinaryService(IOptions<CloudinaryOptions> options)
        {
            var config = options.Value;

            var account = new Account(
                config.CloudName,
                config.ApiKey,
                config.ApiSecret
            );

            _cloudinary = new Cloudinary(account);
        }

        public async Task DeleteImage(string url)
        {
            var publicId = ExtractPublicIdFromUrl(url);

            if (publicId == null) throw new InvalidOperationException($"L?i khi tr�ch xu?t publicId t? URL: {url}");

            if (!string.IsNullOrEmpty(publicId))
            {
                var deleteParams = new DeletionParams(publicId);
                await _cloudinary.DestroyAsync(deleteParams);
            }
        }

        public async Task<string> UploadImage(IFormFile file, string folder)
        {
            if (file == null || file.Length == 0) throw new FileNotFoundException("File ?nh kh�ng du?c d? tr?ng");

            if (file.Length > MaxFileSizeImage) throw new InvalidOperationException($"?nh kh�ng vu?t qu� {MaxFileSizeImage / (1024 * 1024)} MB");

            var fileExtension = Path.GetExtension(file.FileName);

            if (!allowedExtensionsImage.Contains(fileExtension)) throw new ArgumentException($"H�y upload c�c file c� du�i {string.Join(",", allowedExtensionsImage)}");

            using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folder
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            //Console.WriteLine(result);
            return result.SecureUrl.ToString();
        }


        public async Task<string> UploadDocument(IFormFile file, string folder)
        {
            if (file == null || file.Length == 0) throw new FileNotFoundException("File kh�ng du?c d? tr?ng");

            if (file.Length > MaxFileSizeDocument) throw new InvalidOperationException($"T�i li?u kh�ng vu?t qu� {MaxFileSizeDocument / (1024 * 1024)} MB");

            var fileExtension = Path.GetExtension(file.FileName);

            if (!allowedExtensionsDocument.Contains(fileExtension)) throw new ArgumentException($"H�y upload c�c file c� du�i {string.Join(",", allowedExtensionsDocument)}");

            using var stream = file.OpenReadStream();

            var uploadParams = new RawUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folder,
                Type = "upload",
                AccessMode = "public"
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result == null || result.SecureUrl == null)
                throw new InvalidOperationException("Upload t�i li?u th?t b?i: kh�ng nh?n du?c response t? Cloudinary");

            //Console.WriteLine(result);
            return result.SecureUrl.ToString();
        }

        public async Task<DocumentUploadResult> UploadDocumentWithHashAsync(IFormFile file, string folder)
        {
            if (file == null || file.Length == 0) throw new FileNotFoundException("File không được để trống");

            if (file.Length > MaxFileSizeDocument) throw new InvalidOperationException($"Tài liệu không vượt quá {MaxFileSizeDocument / (1024 * 1024)} MB");

            var fileExtension = Path.GetExtension(file.FileName);

            if (!allowedExtensionsDocument.Contains(fileExtension)) throw new ArgumentException($"Hãy upload các file có đuôi {string.Join(",", allowedExtensionsDocument)}");

            // Copy file to memory stream để có thể đọc nhiều lần
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            // 1. Tính hash TRƯỚC khi upload
            string fileHash;
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var hashBytes = await sha256.ComputeHashAsync(memoryStream);
                fileHash = Convert.ToHexString(hashBytes).ToLowerInvariant();
            }

            // Reset stream để upload
            memoryStream.Position = 0;

            // 2. Upload file lên Cloudinary với public access
            var uploadParams = new RawUploadParams
            {
                File = new FileDescription(file.FileName, memoryStream),
                Folder = folder,
                Type = "upload",
                AccessMode = "public" // Public access để tránh 401 khi verify
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result == null || result.SecureUrl == null)
                throw new InvalidOperationException("Upload tài liệu thất bại: không nhận được response từ Cloudinary");

            return new DocumentUploadResult
            {
                FileUrl = result.SecureUrl.ToString(),
                FileHash = fileHash,
                HashAlgorithm = "SHA-256"
            };
        }

        #region helper method
        private string ExtractPublicIdFromUrl(string imageUrl)
        {
            var uri = new Uri(imageUrl);
            var path = uri.AbsolutePath; // /dvdv4id16/image/upload/v1749660746/pho_hk86qj.jpg

            // T�ch ph?n sau "upload/"
            var parts = path.Split("/upload/");

            if (parts.Length < 2)
                throw new ArgumentException("File kh�ng h?p l?");

            // L?y ph?n sau upload/, lo?i b? version
            var pathAfterUpload = parts[1]; // v1749660746/pho_hk86qj.jpg
            var segments = pathAfterUpload.Split('/').ToList();

            if (segments[0].StartsWith("v") && segments[0].Length > 1)
            {
                segments.RemoveAt(0); // b? "v1749660746"
            }

            var fullPath = string.Join("/", segments); // "pho_hk86qj.jpg" ho?c "folder/abc.jpg"
            var publicId = Path.ChangeExtension(fullPath, null); // remove .jpg

            return publicId;
        }
        #endregion
    }
}
