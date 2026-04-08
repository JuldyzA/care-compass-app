using TeamYellow.Models;
using TeamYellow.Repositories;

namespace TeamYellow.Workers
{
    /// <summary>
    /// Background worker that checks for expired subscriptions and marks them as expired.
    /// </summary>
    public class SubscriptionExpiryWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SubscriptionExpiryWorker> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionExpiryWorker"/> class.
        /// </summary>
        /// <param name="scopeFactory">Factory for creating service scopes.</param>
        /// <param name="logger">Logger instance.</param>
        public SubscriptionExpiryWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<SubscriptionExpiryWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }
        /// <summary>
        /// Executes the background service, checking for expired subscriptions at scheduled intervals.
        /// </summary>
        /// <param name="stoppingToken">Token to signal when the service should stop.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SubscriptionExpiryWorker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ExpireSubscriptionsAsync(stoppingToken);
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during expiry check. Will retry in approximately 1 minute.");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }

            _logger.LogInformation("SubscriptionExpiryWorker stopped.");
        }

        /// <summary>
        /// Queries for active subscriptions whose billing cycle has ended, marks them as expired,
        /// and downgrades affected users to Registered_Visitor only when they do not already
        /// have another valid active subscription.
        /// </summary>
        /// <param name="stoppingToken">Token to signal when the service should stop.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task ExpireSubscriptionsAsync(CancellationToken stoppingToken)
        {
            _logger.LogDebug("Running expiry check...");

            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<ISubscriptionRepository>();
            var userRoleRepo = scope.ServiceProvider.GetRequiredService<UserRoleRepository>();

            var expired = await repo.GetActiveExpiredSubscriptionsAsync();

            if (!expired.Any())
            {
                _logger.LogDebug("No expired subscriptions found.");
                return;
            }

            foreach (var sub in expired)
            {
                sub.Status = SubscriptionStatus.Expired;
                _logger.LogInformation("Subscription {SubscriptionId} (Counsellor {CounsellorId}) marked as Expired.", sub.SubscriptionId, sub.CounsellorId);
            }
            await repo.BulkUpdateSubscriptionsAsync(expired);

            var utcNow = DateTime.UtcNow;

            var counsellorIds = expired.Select(s => s.CounsellorId)
                .Distinct()
                .ToList();

            var activeSubscriptionsByCounsellorId = await repo.GetValidActiveSubscriptionsByCounsellorIdsAsync(counsellorIds, utcNow);

            foreach (var sub in expired)
            {   
                if (activeSubscriptionsByCounsellorId.TryGetValue(sub.CounsellorId, out var currentActive))
                {
                    _logger.LogInformation("Skipping role downgrade for CounsellorId {CounsellorId} because active subscription {SubscriptionId} exists until {CycleEnd}", 
                        sub.CounsellorId, currentActive!.SubscriptionId, currentActive.CycleEnd);
                    continue;
                }

                var email = sub.Counsellor?.User?.Email;
                if (email == null)
                {
                    _logger.LogWarning("Could not find email for CounsellorId {CounsellorId}, cannot perform role downgrade.", sub.CounsellorId);
                    continue;
                }

                var downgraded = await userRoleRepo.DowngradeCounsellorToRegisteredVisitorAsync(email);

                if (downgraded)
                {
                    _logger.LogInformation("Role downgraded to Registered_Visitor for CounsellorId {CounsellorId}", sub.CounsellorId);
                }
                else
                {
                    _logger.LogWarning("Role downgrade to Registered_Visitor for CounsellorId {CounsellorId} may be incomplete.", sub.CounsellorId);
                }
            }

            _logger.LogInformation("Expired {Count} subscription(s).", expired.Count);
        }
    }
}
