using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AISEP.Application.QueryParams
{
    public class InvestorQueryParams : BaseQueryParams
    {
        /// <summary>Search by investor name, firm name, or title (replaces base Key)</summary>
        public string? Keyword { get; set; }

        /// <summary>Filter by preferred industry name (partial match)</summary>
        public string? Industry { get; set; }

        /// <summary>Filter by preferred stage (e.g. "Seed", "SeriesA")</summary>
        public string? Stage { get; set; }

        /// <summary>Filter investors whose max ticket >= this value</summary>
        public decimal? TicketSizeMin { get; set; }

        /// <summary>Filter investors whose min ticket <= this value</summary>
        public decimal? TicketSizeMax { get; set; }

        /// <summary>Filter by investor country (partial match)</summary>
        public string? Country { get; set; }

        /// <summary>Filter by KYC investor category: "INDIVIDUAL_ANGEL" | "INSTITUTIONAL"</summary>
        public string? InvestorType { get; set; }

        /// <summary>If true, only return KYC-verified investors (InvestorTag != None)</summary>
        public bool? KycVerified { get; set; }

        /// <summary>Sort order: "latest" | "ticketSizeAsc" | "ticketSizeDesc" | "connectionsDesc"</summary>
        public string? SortBy { get; set; }
    }
}
