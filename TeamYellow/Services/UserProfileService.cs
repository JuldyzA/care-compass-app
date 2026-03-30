using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using TeamYellow.Helpers;
using TeamYellow.Models;
using TeamYellow.Repositories;
using TeamYellow.ViewModels;

namespace TeamYellow.Services;

/// <summary>
/// Service providing user profile business logic for retrieval and updates.
/// </summary>
public class UserProfileService
{
    private readonly UserProfileRepository _repository;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ILogger<UserProfileService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserProfileService"/> class.
    /// </summary>
    /// <param name="repository">Provides data access for user profiles.</param>
    /// <param name="userManager">Manages user identity operations.</param>
    /// <param name="logger">Logs service operations.</param>
    public UserProfileService(UserProfileRepository repository, UserManager<IdentityUser> userManager, ILogger<UserProfileService> logger)
    {
        _repository = repository;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves the user profile view model for the authenticated user.
    /// </summary>
    /// <param name="user">The current authenticated user.</param>
    /// <returns>A user profile view model if found; otherwise <c>null</c>.</returns>
    public async Task<UserProfileVM?> GetProfileAsync(ClaimsPrincipal user)
    {
        UserProfile? profile = await GetUserProfileAsync(user, "retrieve profile");

        if (profile == null)
        {
            return null;
        }

        UserProfileVM vm = UserHelper.MapToVM(profile, user.Identity?.Name);

        return vm;
    }

    /// <summary>
    /// Updates the user profile with the provided view model data.
    /// </summary>
    /// <param name="vm">The user profile view model containing updated values.</param>
    /// <param name="user">The current authenticated user.</param>
    /// <returns><c>true</c> if the update was successful; otherwise <c>false</c>.</returns>
    public async Task<bool> UpdateProfileAsync(UserProfileVM vm, ClaimsPrincipal user)
    {
        UserProfile? profile = await GetUserProfileAsync(user, "update profile");

        if (profile == null)
        {
            return false;
        }

        UserHelper.UpdateEntity(profile, vm);

        return await _repository.UpdateAsync(profile);
    }

    /// <summary>
    /// Retrieves the user profile for the authenticated user with centralized error handling.
    /// </summary>
    /// <param name="user">The current authenticated user.</param>
    /// <param name="operation">Description of the operation being performed (for logging).</param>
    /// <returns>The user profile if found; otherwise <c>null</c>.</returns>
    private async Task<UserProfile?> GetUserProfileAsync(ClaimsPrincipal user, string operation)
    {
        string? userId = _userManager.GetUserId(user);

        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("No user ID found in claims during {Operation}.", operation);
            return null;
        }

        UserProfile? profile = await _repository.GetByUserIdAsync(userId);

        if (profile == null)
        {
            _logger.LogWarning("User profile not found for user ID: {UserId} during {Operation}.", userId, operation);
            return null;
        }

        return profile;
    }
}