using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AISEP.Application.Configuration
{
    public class CloudinaryOptions
    {
        public string CloudName { get; set; } = null!;
        public string ApiKey { get; set; } = null!;
        public string ApiSecret { get; set; } = null!;
        public int SignedUrlExpirationMinutes { get; set; } = 5;
    }
}
