using Microsoft.AspNetCore.Http;
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
        public Task<DocumentUploadResult> UploadDocumentWithHashAsync(IFormFile file, string folder);
    }

    /// <summary>
    /// Result of document upload containing both URL and SHA-256 hash
    /// </summary>
    public class DocumentUploadResult
    {
        public string FileUrl { get; set; } = string.Empty;
        public string FileHash { get; set; } = string.Empty;
        public string HashAlgorithm { get; set; } = "SHA-256";
    }
}
