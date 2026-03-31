using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using TeamYellow.Configurations;
using TeamYellow.DTOs;
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
    private readonly IAzureBlobStorageService _blobStorageService;
    private readonly ILogger<UserProfileService> _logger;
    private readonly string _profilePicturesContainer;
    private const long MAX_FILE_SIZE_BYTES = 5 * 1024 * 1024; // 5 MB

    /// <summary>
    /// Initializes a new instance of the <see cref="UserProfileService"/> class.
    /// </summary>
    /// <param name="repository">Provides data access for user profiles.</param>
    /// <param name="userManager">Manages user identity operations.</param>
    /// <param name="blobStorageService">Provides Azure Blob Storage operations.</param>
    /// <param name="azureStorageConfig">Provides Azure Storage configuration.</param>
    /// <param name="logger">Logs service operations.</param>
    public UserProfileService(
        UserProfileRepository repository,
        UserManager<IdentityUser> userManager,
        IAzureBlobStorageService blobStorageService,
        AzureStorageConfiguration azureStorageConfig,
        ILogger<UserProfileService> logger)
    {
        _repository = repository;
        _userManager = userManager;
        _blobStorageService = blobStorageService;
        _logger = logger;
        _profilePicturesContainer = azureStorageConfig.ProfilePicturesContainer;
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
    /// Retrieves the user account credentials for the authenticated user.
    /// </summary>
    /// <param name="user">The current authenticated user.</param>
    /// <returns>A user account view model with email and masked password; otherwise <c>null</c>.</returns>
    public async Task<UserAccountVM?> GetAccountAsync(ClaimsPrincipal user)
    {
        IdentityUser? identityUser = await _userManager.GetUserAsync(user);

        if (identityUser == null)
        {
            _logger.LogWarning("Identity user not found during retrieve account.");
            return null;
        }

        UserAccountVM vm = UserHelper.MapToVM(identityUser);

        return vm;
    }

    /// <summary>
    /// Uploads a profile image for the specified user and returns the blob URL.
    /// </summary>
    /// <param name="dto">The profile image upload DTO.</param>
    /// <param name="user">The current authenticated user.</param>
    /// <returns>
    /// (success, errorMessage, imageUrl)
    /// </returns>
    public async Task<(bool Success, string? ErrorMessage, string? ImageUrl)> UploadProfileImageAsync
    (
        ProfileImageUploadDto dto,
        ClaimsPrincipal user
    ) {
        try
        {
            if (string.IsNullOrWhiteSpace(dto?.ImageData))
            {
                return (false, ImageUploadMessages.NoImageDataProvided, null);
            }

            // Validate base64 data format
            if (!dto.ImageData.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
            {
                return (false, ImageUploadMessages.InvalidImageDataFormat, null);
            }

            // Extract base64 content from data URL
            string[] parts = dto.ImageData.Split(',');
            if (parts.Length != 2)
            {
                return (false, ImageUploadMessages.InvalidImageDataFormat, null);
            }

            string base64Data = parts[1];
            byte[] imageBytes;

            try
            {
                imageBytes = Convert.FromBase64String(base64Data);
            }
            catch (FormatException)
            {
                return (false, ImageUploadMessages.InvalidBase64Data, null);
            }

            // Validate file size
            if (imageBytes.Length > MAX_FILE_SIZE_BYTES)
            {
                return (false, $"{ImageUploadMessages.FileSizeExceedsPrefix} {MAX_FILE_SIZE_BYTES / (1024 * 1024)}MB limit.", null);
            }

            string? userId = _userManager.GetUserId(user);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return (false, ImageUploadMessages.UserNotIdentified, null);
            }

            string fileName = $"profile-{userId}-{DateTime.UtcNow:yyyyMMddHHmmss}.jpg";

            using MemoryStream memoryStream = new MemoryStream(imageBytes);
            string blobUrl = await _blobStorageService.UploadFileAsync(memoryStream, fileName, _profilePicturesContainer);

            _logger.LogInformation("Profile image uploaded successfully for user {UserId}", userId);

            return (true, null, blobUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading profile image");
            return (false, ImageUploadMessages.UploadError, null);
        }
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