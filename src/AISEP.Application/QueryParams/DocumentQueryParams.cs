using AISEP.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AISEP.Application.QueryParams
{
    public class DocumentQueryParams : BaseQueryParams
    {
        public DocumentType? DocumentType { get; set; }
    }
}
