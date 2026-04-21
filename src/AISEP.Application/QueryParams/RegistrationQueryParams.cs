using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AISEP.Application.QueryParams
{
    public class RegistrationQueryParams : BaseQueryParams
    {
    }

    public class RegistrationHistoryQueryParams : BaseQueryParams
    {
        public string? RoleType { get; set; }   // STARTUP | ADVISOR | INVESTOR
        public string? Result { get; set; }     // APPROVED | REJECTED
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }
}
