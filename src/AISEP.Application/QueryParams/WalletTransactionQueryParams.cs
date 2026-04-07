using AISEP.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AISEP.Application.QueryParams
{
    public class WalletTransactionQueryParams : BaseQueryParams
    {
        [JsonPropertyName("transactionType")]   
        
        public TransactionType? TransactionType { get; set; }

        [JsonPropertyName("transactionStatus")]
        public TransactionStatus? TransactionStatus { get; set; }
    }
}
