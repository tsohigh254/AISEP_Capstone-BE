using AISEP.Domain.Enums;

namespace AISEP.Application.DTOs.Payment;

public class SubscriptionPaymentRequestDto
{
    public StartupSubscriptionPlan TargetPlan { get; set; }
    public int Amount { get; set; }
}
