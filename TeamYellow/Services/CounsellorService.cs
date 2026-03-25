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
        private readonly CounsellorRepository _repository;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<CounsellorService> _logger;

        public CounsellorService(
            CounsellorRepository repository,
            UserManager<IdentityUser> userManager,
            ILogger<CounsellorService> logger
        ) {
            _repository = repository;
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
                return new CounsellorDashboardVM();
            }

            CounsellorDashboardDto dto = await _repository.GetCounsellorDashboardDtoAsync(userId);

            dto.IsSubscriptionActive = dto.CycleEnd > DateTime.UtcNow && dto.Status == SubscriptionStatus.Active;

            CounsellorDashboardVM vm = CounsellorDashboardHelper.MapToVm(dto, userId);

            return vm;
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

            Counsellor? counsellor = await _repository.GetByUserIdAsync(userId);

            if (counsellor == null)
            {
                _logger.LogWarning("No counsellor record found for user ID {UserId}.", userId);
            }

            return counsellor;
        }
    }
}