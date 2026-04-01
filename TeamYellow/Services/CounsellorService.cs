using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using TeamYellow.DTOs;
using TeamYellow.Helpers;
using TeamYellow.Models;
using TeamYellow.Repositories;
using TeamYellow.ViewModels;

namespace TeamYellow.Services
{
    /// <summary>
    /// Service that implements counsellor-related business logic for dashboard and billing data retrieval.
    /// </summary>
    public class CounsellorService
    {
        private readonly CounsellorRepository _counsellorRepository;
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly UserRoleRepository _userRoleRepository;
        private readonly UserProfileRepository _userProfileRepository;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<CounsellorService> _logger;

        public CounsellorService(
            CounsellorRepository counsellorRepository,
            ISubscriptionRepository subscriptionRepository,
            UserRoleRepository userRoleRepository,
            UserProfileRepository userProfileRepository,
            UserManager<IdentityUser> userManager,
            ILogger<CounsellorService> logger
        ) {
            _counsellorRepository = counsellorRepository;
            _subscriptionRepository = subscriptionRepository;
            _userRoleRepository = userRoleRepository;
            _userProfileRepository = userProfileRepository;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves dashboard data for the currently authenticated counsellor.
        /// </summary>
        /// <param name="user">The current authenticated user.</param>
        /// <returns>A counsellor dashboard view model.</returns>
        public async Task<CounsellorDashboardVM> GetCounsellorDashboardAsync(ClaimsPrincipal user)
        {
            string? userId = _userManager.GetUserId(user);

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unable to extract user ID from claims.");
                return new CounsellorDashboardVM
                {
                    IsDashboardLocked = true
                };
            }

            CounsellorDashboardDto dto = await _counsellorRepository.GetCounsellorDashboardDtoAsync(userId);

            dto.IsSubscriptionActive = dto.CycleEnd > DateTime.UtcNow && dto.Status == SubscriptionStatus.Active;

            CounsellorDashboardVM vm = CounsellorDashboardHelper.MapToVm(dto, userId);

            return vm;
        }

        /// <summary>
        /// Evaluates whether the current counsellor user should be locked out of counsellor pages
        /// because the active subscription is missing or expired.
        /// </summary>
        /// <param name="user">The current authenticated user.</param>
        /// <returns>
        /// A tuple containing lock state, sign-in refresh requirement, optional error message,
        /// profile photo URL, and display name.
        /// </returns>
        public async Task<(bool IsLocked, bool ShouldRefreshSignIn, string? ErrorMessage, string? ProfilePhotoUrl, string DisplayName)> GetCounsellorPageAccessStateAsync(ClaimsPrincipal user)
        {
            string? userId = _userManager.GetUserId(user);

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unable to extract user ID from claims.");
                return (true, false, "We could not verify your identity. Please sign out and sign in again.", null, string.Empty);
            }

            Counsellor? counsellor = await _counsellorRepository.GetByUserIdAsync(userId);
            var (profile, _) = await _userProfileRepository.GetByUserIdAsync(userId);

            string displayName = counsellor?.DisplayName ?? string.Empty;
            string? profilePhotoUrl = profile?.ProfilePhotoUrl;

            if (user.IsInRole("Registered_Visitor"))
            {
                return (true, false, null, profilePhotoUrl, displayName);
            }

            if (counsellor == null)
            {
                _logger.LogWarning("No counsellor record found for user ID {UserId}.", userId);
                return (true, false, "We could not load your counsellor profile. Please sign out and sign in again.", profilePhotoUrl, displayName);
            }

            var activeSubscription = await _subscriptionRepository.GetActiveSubscriptionByCounsellorId(counsellor.CounsellorId);

            bool shouldLock = activeSubscription == null ||
                activeSubscription.Status == SubscriptionStatus.Expired ||
                activeSubscription.CycleEnd <= DateTime.UtcNow;

            if (!shouldLock)
            {
                return (false, false, null, profilePhotoUrl, displayName);
            }

            var identityUser = await _userManager.GetUserAsync(user);

            if (identityUser == null || string.IsNullOrWhiteSpace(identityUser.Email))
            {
                _logger.LogWarning("Locked counsellor access detected but the Identity user/email could not be resolved.");
                return (true, false, "We could not refresh your subscription access automatically. Please sign out and sign in again.", profilePhotoUrl, displayName);
            }

            bool downgraded = await _userRoleRepository.DowngradeCounsellorToRegisteredVisitorAsync(identityUser.Email);

            if (downgraded)
            {
                _logger.LogInformation("Expired or missing subscription detected for user {Email}. Sign-in refresh is required.", identityUser.Email);
                return (true, true, null, profilePhotoUrl, displayName);
            }

            _logger.LogWarning("Expired or missing subscription detected for user {Email}, but role downgrade was unsuccessful.", identityUser.Email);
            return (true, false, "We could not refresh your subscription access automatically. Please sign out and sign in again.", profilePhotoUrl, displayName);
        }

        /// <summary>
        /// Retrieves the counsellor record associated with the authenticated user.
        /// </summary>
        /// <param name="user">The current authenticated user.</param>
        /// <returns>The matching counsellor entity, or <c>null</c> if not found.</returns>
        public async Task<Counsellor?> GetCounsellorByUser(ClaimsPrincipal user)
        {
            string? userId = _userManager.GetUserId(user);

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unable to extract user ID from claims.");
                return null;
            }

            Counsellor? counsellor = await _counsellorRepository.GetByUserIdAsync(userId);

            if (counsellor == null)
            {
                _logger.LogWarning("No counsellor record found for user ID {UserId}.", userId);
            }

            return counsellor;
        }

        /// <summary>
        /// Retrieves billing information and transaction history for the authenticated counsellor.
        /// Internally resolves the counsellor from the current user context and maps subscription data to view models.
        /// </summary>
        /// <param name="user">The current authenticated user.</param>
        /// <returns>A counsellor billing view model with transaction history, or null if counsellor not found.</returns>
        public async Task<CounsellorBillingVM?> GetCounsellorBillingDataAsync(ClaimsPrincipal user)
        {
            Counsellor? counsellor = await GetCounsellorByUser(user);

            if (counsellor == null)
            {
                _logger.LogWarning("Cannot retrieve billing data because counsellor profile could not be found for the current user.");
                return null;
            }

            List<Subscription> subscriptions = await _counsellorRepository.GetCounsellorSubscriptionsAsync(counsellor.CounsellorId);

            CounsellorBillingVM vm = CounsellorDashboardHelper.CreateEmptyBillingVM();
            PopulateBillingTransactions(vm, subscriptions);

            return vm;
        }

        /// <summary>
        /// Private helper method that populates a billing view model with transaction data from subscriptions.
        /// Calculates active subscription status, individual transactions, and summary totals.
        /// </summary>
        /// <param name="billingVM">The billing view model to populate.</param>
        /// <param name="subscriptions">The collection of subscriptions to extract transactions from.</param>
        private void PopulateBillingTransactions(CounsellorBillingVM billingVM, List<Subscription> subscriptions)
        {
            if (billingVM == null || subscriptions == null)
            {
                return;
            }

            // Check if there's an active subscription
            Subscription? activeSubscription = subscriptions.FirstOrDefault(s => s.Status == SubscriptionStatus.Active && s.CycleEnd > DateTime.UtcNow);
            billingVM.IsSubscriptionActive = activeSubscription != null;
            billingVM.CurrentCycleEnd = activeSubscription?.CycleEnd;

            // Process all subscriptions with payment transactions
            foreach (Subscription subscription in subscriptions.Where(s => s.PaymentTransaction != null))
            {
                PaymentTransaction paymentTransaction = subscription.PaymentTransaction!;
                BillingTransactionVM transaction = CounsellorDashboardHelper.MapToTransactionVM(paymentTransaction, subscription);
                billingVM.Transactions.Add(transaction);
            }

            // Calculate totals
            billingVM.TotalTransactions = billingVM.Transactions.Count;
            billingVM.TotalSpent = billingVM.Transactions
                .Where(t => t.Status == "Paid")
                .Sum(t => t.Amount);
        }
    }
}