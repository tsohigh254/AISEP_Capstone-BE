using Microsoft.AspNetCore.Http;
using AISEP.Application.DTOs.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AISEP.Application.Interfaces
{
    public interface ICloudinaryService
    {
        public Task DeleteImage(string url);
        public Task<string> UploadImage(IFormFile file, string folder);
        public Task<string> UploadDocument(IFormFile file, string folder);
        public Task<CloudinaryUploadResultDto> UploadDocumentWithMetadata(IFormFile file, string folder);
        public string GenerateSignedDocumentUrl(string? storageKey, string? fallbackUrl = null, string? fileName = null, int? expiresInMinutes = null);
        public string? ExtractDocumentStorageKeyFromUrl(string? url);
    }
}
