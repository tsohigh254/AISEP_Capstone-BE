using AISEP.Domain.Enums;
using AISEP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AISEP.Infrastructure.Jobs;

public class SubscriptionExpirationJob
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<SubscriptionExpirationJob> _logger;

    public SubscriptionExpirationJob(ApplicationDbContext db, ILogger<SubscriptionExpirationJob> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task ProcessExpiredSubscriptions()
    {
        _logger.LogInformation("Starting ProcessExpiredSubscriptions job at {time}", DateTime.UtcNow);

        var expiredStartups = await _db.Startups
            .Where(s => s.SubscriptionPlan != StartupSubscriptionPlan.Free 
                        && s.SubscriptionEndDate.HasValue 
                        && s.SubscriptionEndDate.Value < DateTime.UtcNow)
            .ToListAsync();

        if (expiredStartups.Count == 0)
        {
            _logger.LogInformation("No expired subscriptions found.");
            return;
        }

        foreach (var startup in expiredStartups)
        {
            _logger.LogInformation("Downgrading StartupId: {id} from plan {plan} to Free.", startup.StartupID, startup.SubscriptionPlan);
            startup.SubscriptionPlan = StartupSubscriptionPlan.Free;
            startup.SubscriptionEndDate = null;
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("Successfully processed {count} expired subscriptions.", expiredStartups.Count);
    }
}
