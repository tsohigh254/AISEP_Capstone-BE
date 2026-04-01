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
    }
}
