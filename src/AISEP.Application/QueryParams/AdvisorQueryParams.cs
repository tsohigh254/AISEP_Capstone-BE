using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AISEP.Application.QueryParams
{
    public class AdvisorQueryParams : BaseQueryParams
    {
        /// <summary>Text search on name, title, bio, expertise. Alias for BaseQueryParams.Key.</summary>
        public string? Search { get; set; }
        /// <summary>Filter by expertise keyword (e.g. "MARKETING").</summary>
        public string? Expertise { get; set; }
        /// <summary>Minimum years of experience (e.g. 5 = ≥5 years).</summary>
        public int? Experience { get; set; }
        /// <summary>Minimum average rating (e.g. 4 = ≥4 stars).</summary>
        public float? Rating { get; set; }
        /// <summary>Sort order: best_match | rating_desc | newest (default).</summary>
        public string? Sort { get; set; }
        /// <summary>When true, include advisors who are not accepting new mentees. Default: false.</summary>
        public bool IncludeUnavailable { get; set; } = false;
    }
}
