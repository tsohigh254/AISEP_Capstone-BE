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
        private readonly string[] allowedExtensionsImage = { ".jpeg", ".png", ".jpg", ".webp" };
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

            if (publicId == null) throw new InvalidOperationException($"Lỗi khi trích xuất publicId từ URL: {url}");

            if (!string.IsNullOrEmpty(publicId))
            {
                var deleteParams = new DeletionParams(publicId);
                await _cloudinary.DestroyAsync(deleteParams);
            }
        }

        public async Task<string> UploadImage(IFormFile file, string folder)
        {
            if (file == null || file.Length == 0) throw new FileNotFoundException("File ảnh không được để trống");

            if (file.Length > MaxFileSizeImage) throw new InvalidOperationException($"Ảnh không vượt quá {MaxFileSizeImage / (1024 * 1024)} MB");

            var fileExtension = Path.GetExtension(file.FileName).ToLower();

            if (!allowedExtensionsImage.Contains(fileExtension)) throw new ArgumentException($"Hãy upload các file có đuôi {string.Join(",", allowedExtensionsImage)}");

            using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folder
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result.Error != null)
                throw new InvalidOperationException($"Cloudinary upload failed: {result.Error.Message}");

            return result.SecureUrl.ToString();
        }


        public async Task<string> UploadDocument(IFormFile file, string folder)
        {
            if (file == null || file.Length == 0) throw new FileNotFoundException("File không được để trống");

            if (file.Length > MaxFileSizeDocument) throw new InvalidOperationException($"Tài liệu không vượt quá {MaxFileSizeDocument / (1024 * 1024)} MB");

            var fileExtension = Path.GetExtension(file.FileName).ToLower();

            if (!allowedExtensionsDocument.Contains(fileExtension)) throw new ArgumentException($"Hãy upload các file có đuôi {string.Join(",", allowedExtensionsDocument)}");

            using var stream = file.OpenReadStream();

            var uploadParams = new RawUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folder,
                //ResourceType = "raw"
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result.Error != null)
                throw new InvalidOperationException($"Cloudinary upload failed: {result.Error.Message}");

            if (result == null || result.SecureUrl == null)
                throw new InvalidOperationException("Upload tài liệu thất bại: không nhận được response từ Cloudinary");

            //Console.WriteLine(result);
            return result.SecureUrl.ToString();
        }

        #region helper method
        private string ExtractPublicIdFromUrl(string imageUrl)
        {
            var uri = new Uri(imageUrl);
            var path = uri.AbsolutePath; // /dvdv4id16/image/upload/v1749660746/pho_hk86qj.jpg

            // Tách phần sau "upload/"
            var parts = path.Split("/upload/");

            if (parts.Length < 2)
                throw new ArgumentException("File không hợp lệ");

            // Lấy phần sau upload/, loại bỏ version
            var pathAfterUpload = parts[1]; // v1749660746/pho_hk86qj.jpg
            var segments = pathAfterUpload.Split('/').ToList();

            if (segments[0].StartsWith("v") && segments[0].Length > 1)
            {
                segments.RemoveAt(0); // bỏ "v1749660746"
            }

            var fullPath = string.Join("/", segments); // "pho_hk86qj.jpg" hoặc "folder/abc.jpg"
            var publicId = Path.ChangeExtension(fullPath, null); // remove .jpg

            return publicId;
        }
        #endregion
    }
}
