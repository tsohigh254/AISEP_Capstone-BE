using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AISEP.Application.DTOs.QueryParams
{
    public class AdvisorQueryParams : BaseQueryParams
    {
        [JsonPropertyName("industry")]

        public int? Industry { get; set; }
    }
}
