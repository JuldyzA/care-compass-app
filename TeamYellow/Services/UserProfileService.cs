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
    private const long MAX_FILE_SIZE_BYTES = FileUploadConfiguration.MAX_PROFILE_IMAGE_SIZE_BYTES;

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
    /// Retrieves the user profile view model for the authenticated user with optional counsellor display name.
    /// </summary>
    /// <param name="user">The current authenticated user.</param>
    /// <returns>A user profile view model if found; otherwise <c>null</c>.</returns>
    public async Task<UserProfileVM?> GetProfileAsync(ClaimsPrincipal user)
    {
        string? userId = _userManager.GetUserId(user);

        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("No user ID found in claims during retrieve profile.");
            return null;
        }

        var (profile, counsellorDisplayName) = await _repository.GetByUserIdAsync(userId);

        if (profile == null)
        {
            _logger.LogWarning("User profile not found for user ID: {UserId} during retrieve profile.", userId);
            return null;
        }

        UserProfileVM vm = UserHelper.MapToVM(profile, user.Identity?.Name, counsellorDisplayName);

        return vm;
    }

    /// <summary>
    /// Updates the user profile with the provided view model data.
    /// For paid and free counsellor roles, also updates the DisplayName in the Counsellor table.
    /// Uses a transaction with rollback on failure.
    /// </summary>
    /// <param name="vm">The user profile view model containing updated values.</param>
    /// <param name="user">The current authenticated user.</param>
    /// <returns><c>true</c> if the update was successful; otherwise <c>false</c>.</returns>
    public async Task<bool> UpdateProfileAsync(UserProfileVM vm, ClaimsPrincipal user)
    {
        string? userId = _userManager.GetUserId(user);

        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("No user ID found in claims during update profile.");
            return false;
        }

        var (profile, _) = await _repository.GetByUserIdAsync(userId);

        if (profile == null)
        {
            _logger.LogWarning("User profile not found for user ID: {UserId} during update profile.", userId);
            return false;
        }

        // Determine the displayName value based on user role
        // Only Paid and Free Counsellors can update DisplayName
        string? displayNameToUpdate = null;
        
        bool isCounsellor = user.IsInRole("Paid_Counselor") || user.IsInRole("Free_Counselor");

        if (isCounsellor)
        {
            // For counsellors: only update if DisplayName is explicitly provided and non-empty
            string? trimmedDisplayName = vm.DisplayName?.Trim();
            
            if (!string.IsNullOrEmpty(trimmedDisplayName))
            {
                displayNameToUpdate = trimmedDisplayName;
            }
            else
            {
                _logger.LogInformation("DisplayName is empty for counsellor {UserId}. Counsellor DisplayName will not be updated.", userId);
                // displayNameToUpdate remains null, so counsellor table won't be updated
            }
        }

        // For registered visitors, admins, managers, or other roles: displayNameToUpdate remains null
        UserHelper.UpdateEntity(profile, vm);

        return await _repository.UpdateAsync(profile, displayNameToUpdate);
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
}