using System.Composition;
using Microsoft.Extensions.Options;
using TeamYellow.Models;
using TeamYellow.Repositories;

namespace TeamYellow.Workers
{
    /// <summary>
    /// Configuration settings for the background worker.
    /// </summary>
    public class WorkerSettings
    {
        /// <summary>
        /// The hour (0-23) at which the worker should run.
        /// </summary>
        public int RunAtHour { get; set; } = 0;

        /// <summary>
        /// The minute (0-59) at which the worker should run.
        /// </summary>
        public int RunAtMinute { get; set; } = 1;
    }

    /// <summary>
    /// Background worker that checks for expired subscriptions and marks them as expired.
    /// </summary>
    public class SubscriptionExpiryWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SubscriptionExpiryWorker> _logger;
        private readonly WorkerSettings _settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionExpiryWorker"/> class.
        /// </summary>
        /// <param name="scopeFactory">Factory for creating service scopes.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="settings">Worker settings containing scheduled run time.</param>
        public SubscriptionExpiryWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<SubscriptionExpiryWorker> logger,
            IOptions<WorkerSettings> settings)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _settings = settings.Value;
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
                    _logger.LogError(ex, "Error during expiry check. Will retry at the next scheduled run time.");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }

            _logger.LogInformation("SubscriptionExpiryWorker stopped.");
        }

        /// <summary>              
        /// Calculates the delay until the next scheduled run time based on <see cref="WorkerSettings"/>.
        /// </summary>
        /// <returns>A <see cref="TimeSpan"/> representing the time until the next run.</returns>
        private TimeSpan GetDelayUntilNextRun()
        {
            var now = DateTime.UtcNow;
            var nextRun = new DateTime(now.Year, now.Month, now.Day, _settings.RunAtHour, _settings.RunAtMinute, 0, DateTimeKind.Utc);
            if (now > nextRun)
                nextRun = nextRun.AddDays(1);
            return nextRun - now;
        }

        /// <summary>
        /// Queries for active subscriptions with a past cycle end date and marks them as expired.
        /// </summary>
        /// <param name="stoppingToken">Token to signal when the service should stop.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task ExpireSubscriptionsAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Running expiry check...");

            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<ISubscriptionRepository>();
            var userRoleRepo = scope.ServiceProvider.GetRequiredService<UserRoleRepository>();

            var expired = await repo.GetActiveExpiredSubscriptionsAsync();

            if (!expired.Any())
            {
                _logger.LogInformation("No expired subscriptions found.");
                return;
            }

            foreach (var sub in expired)
            {
                sub.Status = SubscriptionStatus.Expired;
                _logger.LogInformation("Subscription {SubscriptionId} (Counsellor {CounsellorId}) marked as Expired.", sub.SubscriptionId, sub.CounsellorId);
            }
            await repo.BulkUpdateSubscriptionsAsync(expired);

            foreach (var sub in expired)
            {
                var email = sub.Counsellor?.User?.Email;
                if (email == null)
                {
                    _logger.LogWarning("Could not find email for CounselorId {CounsellorId}, cannt perform role downgrade.", sub.CounsellorId);
                    continue;
                }

                await userRoleRepo.RemoveUserRoleAsync(email, "Free_Counselor");
                await userRoleRepo.RemoveUserRoleAsync(email, "Paid_Counselor");
                await userRoleRepo.AddUserRoleAsync(email, "Registered_Visitor");
                _logger.LogInformation("Role downgraded to Registered_Visitor for CounsellorId {CounsellorId}", sub.CounsellorId);

            }


            _logger.LogInformation("Expired {Count} subscription(s).", expired.Count);
        }
    }
}
