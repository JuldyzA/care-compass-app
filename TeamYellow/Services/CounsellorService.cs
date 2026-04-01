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
    /// Service that implements counsellor-related business logic for dashboard and profile retrieval.
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
        /// based on their effective role and subscription access state.
        /// Visitor-only users are locked and prompted to activate a subscription.
        /// Counsellor users are locked when their active subscription is missing or expired.
        /// If an expired counsellor is successfully downgraded to Registered_Visitor,
        /// the method signals that the sign-in cookie should be refreshed.
        /// </summary>
        /// <param name="user">The current authenticated user.</param>
        /// <returns>
        /// A tuple containing:
        /// <list type="bullet">
        /// <item><description>Whether counsellor access should be locked.</description></item>
        /// <item><description>Whether the user's sign-in should be refreshed.</description></item>
        /// <item><description>An optional message explaining the current access state.</description></item>
        /// <item><description>The user's profile photo URL, if available.</description></item>
        /// <item><description>The counsellor display name, if available.</description></item>
        /// </list>
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
            UserProfile? profile = await _userProfileRepository.GetByUserIdAsync(userId);

            string displayName = counsellor?.DisplayName ?? string.Empty;
            string? profilePhotoUrl = profile?.ProfilePhotoUrl;

            bool isVisitorOnly = user.IsInRole("Registered_Visitor") &&
                !user.IsInRole("Free_Counselor") && !user.IsInRole("Paid_Counselor");

            if (isVisitorOnly)
            {
                return (true, false, "Please activate your subscription to access the counsellor dashboard.", profilePhotoUrl, displayName);
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
    }
}