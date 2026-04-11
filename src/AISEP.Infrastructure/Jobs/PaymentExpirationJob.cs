using AISEP.Domain.Enums;
using AISEP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AISEP.Infrastructure.Jobs;

public class PaymentExpirationJob
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<PaymentExpirationJob> _logger;

    public PaymentExpirationJob(ApplicationDbContext db, ILogger<PaymentExpirationJob> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task ExpireSubscriptionPayment(int paymentId)
    {
        _logger.LogInformation("Checking expiration for subscription payment {paymentId} at {time}", paymentId, DateTime.UtcNow);

        var payment = await _db.StartupSubscriptionPayments
            .FirstOrDefaultAsync(p => p.PaymentID == paymentId);

        if (payment == null)
        {
            _logger.LogWarning("Subscription payment {paymentId} not found.", paymentId);
            return;
        }

        if (payment.PaymentStatus == PaymentStatus.Pending)
        {
            _logger.LogInformation("Marking subscription payment {paymentId} as Failed.", paymentId);
            payment.PaymentStatus = PaymentStatus.Failed;
            await _db.SaveChangesAsync();
        }
        else
        {
            _logger.LogInformation("Subscription payment {paymentId} has status {status}. No action taken.", paymentId, payment.PaymentStatus);
        }
    }
}
