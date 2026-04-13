using System;
using AISEP.Domain.Enums;

namespace AISEP.Domain.Entities;

public class StartupSubscriptionPayment
{
    public int PaymentID { get; set; }
    public int StartupID { get; set; }
    
    // The plan they are applying or upgrading to.
    public StartupSubscriptionPlan TargetPlan { get; set; }

    public decimal Amount { get; set; }
    public int? TransactionCode { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }

    // Navigation Property
    public Startup Startup { get; set; } = null!;
}
