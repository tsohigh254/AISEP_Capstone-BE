using AISEP.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AISEP.Application.QueryParams
{
    public class StartupQueryParams : BaseQueryParams
    {
        [JsonPropertyName("stage")]   
        
        public StartupStage? Stage { get; set; }
    }
}
