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
        public int? IndustryID { get; set; }
        public int? SubIndustryID { get; set; }
        public int? StageID { get; set; }
    }
}
