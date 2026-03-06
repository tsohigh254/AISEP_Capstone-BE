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
        private readonly string[] allowedExtensions = { ".jpeg", ".gif", ".png", ".jpg" };
        private readonly Cloudinary _cloudinary;
        private const int MaxFileSize = 5 * 1024 * 1024;

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

            if (file.Length > MaxFileSize) throw new InvalidOperationException($"Ảnh không vượt quá {MaxFileSize / (1024 * 1024)} MB");

            var fileExtension = Path.GetExtension(file.FileName);

            if (!allowedExtensions.Contains(fileExtension)) throw new ArgumentException($"Hãy upload các file có đuôi {string.Join(",", allowedExtensions)}");

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
