using Microsoft.Extensions.Options;
using TeamYellow.Models;
using TeamYellow.Repositories;

namespace TeamYellow.Workers
{
    public class WorkerSettings
    {
        public int RunAtHour { get; set; } = 0;
        public int RunAtMinute { get; set; } = 1;
    }

    public class SubscriptionExpiryWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SubscriptionExpiryWorker> _logger;
        private readonly WorkerSettings _settings;

        public SubscriptionExpiryWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<SubscriptionExpiryWorker> logger,
            IOptions<WorkerSettings> settings)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _settings = settings.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SubscriptionExpiryWorker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var delay = GetDelayUntilNextRun();
                    var nextRun = DateTime.UtcNow.Add(delay);
                    _logger.LogInformation("Next scheduled run at {NextRun} UTC (in {Delay}).", nextRun, delay);

                    await Task.Delay(delay, stoppingToken);

                    if (stoppingToken.IsCancellationRequested) break;

                    _logger.LogInformation("Expiry check triggered by: schedule.");
                    await ExpireSubscriptionsAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during expiry check. Retrying in 1 minute.");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }

            _logger.LogInformation("SubscriptionExpiryWorker stopped.");
        }

        private TimeSpan GetDelayUntilNextRun()
        {
            var now = DateTime.UtcNow;
            var nextRun = new DateTime(now.Year, now.Month, now.Day, _settings.RunAtHour, _settings.RunAtMinute, 0, DateTimeKind.Utc);
            if (now >= nextRun)
                nextRun = nextRun.AddDays(1);
            return nextRun - now;
        }

        private async Task ExpireSubscriptionsAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Running expiry check...");

            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<ISubscriptionRepository>();

            var expired = await repo.GetActiveExpiredSubscriptionsAsync();

            if (!expired.Any())
            {
                _logger.LogInformation("No expired subscriptions found.");
                return;
            }

            foreach (var sub in expired)
            {
                sub.Status = SubscriptionStatus.Expired;
                await repo.UpdateSubscription(sub);
                _logger.LogInformation("Subscription {SubscriptionId} (Counsellor {CounsellorId}) marked as Expired.", sub.SubscriptionId, sub.CounsellorId);
            }

            _logger.LogInformation("Expired {Count} subscription(s).", expired.Count);
        }
    }
}
