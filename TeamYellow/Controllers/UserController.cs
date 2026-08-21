using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TeamYellow.Configurations;
using TeamYellow.DTOs;
using TeamYellow.Services;
using TeamYellow.ViewModels;

namespace TeamYellow.Controllers;

/// <summary>
/// Handles user profile display and management workflows for authenticated users.
/// </summary>
[Authorize]
public class UserController : Controller
{
    private readonly UserProfileService _userProfileService;
    private readonly UserAccountDeletionService _userAccountDeletionService;
    private readonly IAzureBlobStorageService _blobStorageService;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserController"/> class.
    /// </summary>
    /// <param name="userProfileService">Provides user profile business logic.</param>
    public UserController(
        UserProfileService userProfileService,
        UserAccountDeletionService userAccountDeletionService,
        IAzureBlobStorageService blobStorageService)
    {
        _userProfileService = userProfileService;
        _userAccountDeletionService = userAccountDeletionService;
        _blobStorageService = blobStorageService;
    }

    /// <summary>
    /// Sets the default page title for user actions before the action executes.
    /// </summary>
    /// <param name="context">The current action-executing context.</param>
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        ViewData["Title"] = "User Profile";
        base.OnActionExecuting(context);
    }

    /// <summary>
    /// Displays the user profile page with current profile information.
    /// </summary>
    /// <returns>
    /// The profile view with populated user profile data when successful; 
    /// otherwise redirects to home page.
    /// </returns>
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        UserProfileVM? profileVM = await _userProfileService.GetProfileAsync(User);

        if (profileVM == null)
        {
            TempData["ErrorMessage"] = "Unable to load your profile. Please try again later.";
            return RedirectToAction("Index", "Home");
        }

        return View(profileVM);
    }

    /// <summary>
    /// Displays the edit form for the user profile.
    /// </summary>
    /// <returns>
    /// The edit view with populated user profile data when successful; 
    /// otherwise redirects to profile page.
    /// </returns>
    [HttpGet]
    public async Task<IActionResult> EditProfile()
    {
        UserProfileVM? profileVM = await _userProfileService.GetProfileAsync(User);

        if (profileVM == null)
        {
            TempData["ErrorMessage"] = "Unable to load your profile. Please try again later.";
            return RedirectToAction(nameof(Profile));
        }

        return View(profileVM);
    }

    /// <summary>
    /// Updates the user profile and redirects back to the profile page.
    /// </summary>
    /// <param name="viewModel">The submitted user profile view model containing updated values.</param>
    /// <returns>
    /// A redirect to the profile page when successful; 
    /// otherwise returns the edit view with validation errors.
    /// </returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProfile(UserProfileVM vm)
    {
        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        bool updated = await _userProfileService.UpdateProfileAsync(vm, User);

        if (!updated)
        {
            ModelState.AddModelError(string.Empty, "An error occurred while updating your profile. Please try again later.");
            return View(vm);
        }

        TempData["SuccessMessage"] = "Your profile has been updated successfully.";
        return RedirectToAction(nameof(Profile));
    }

    /// <summary>
    /// Displays the user account credentials page with email and masked password.
    /// </summary>
    /// <returns>
    /// The account view with user email and masked password; 
    /// otherwise redirects to home page.
    /// </returns>
    [HttpGet]
    public async Task<IActionResult> Account()
    {
        UserAccountVM? vm = await _userProfileService.GetAccountAsync(User);

        if (vm == null)
        {
            TempData["ErrorMessage"] = "Unable to load your account information. Please try again later.";
            return RedirectToAction("Index", "Home");
        }

        return View(vm);
    }

    /// <summary>
    /// Displays the delete account confirmation page.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> DeleteAccount()
    {
        if (!CanCurrentUserSelfDelete())
        {
            return Forbid();
        }

        UserAccountVM? accountVM = await _userProfileService.GetAccountAsync(User);

        if (accountVM == null)
        {
            TempData["ErrorMessage"] = "Unable to load your account information. Please try again later.";
            return RedirectToAction(nameof(Account));
        }

        DeleteAccountVM vm = new DeleteAccountVM
        {
            Email = accountVM.Email
        };

        return View(vm);
    }

    /// <summary>
    /// Handles the confirmed delete account request.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAccount(DeleteAccountVM vm)
    {
        if (!CanCurrentUserSelfDelete())
        {
            return Forbid();
        }

        UserAccountVM? accountVM = await _userProfileService.GetAccountAsync(User);

        if (accountVM == null)
        {
            TempData["ErrorMessage"] = "Unable to load your account information. Please try again later.";
            return RedirectToAction(nameof(Account));
        }

        vm.Email = accountVM.Email;

        if (!string.Equals(vm.ConfirmationText?.Trim(), "DELETE", StringComparison.Ordinal))
        {
            ModelState.AddModelError(nameof(vm.ConfirmationText), "Type DELETE exactly to confirm.");
        }

        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var result = await _userAccountDeletionService.DeleteCurrentUserAsync(User);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(vm);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction("Index", "Home");
    }

    /// <summary>
    /// Uploads a cropped profile image to Azure Blob Storage.
    /// </summary>
    /// <param name="dto">The profile image upload data transfer object.</param>
    /// <returns>
    /// A JSON response containing the new image URL on success; 
    /// otherwise returns an error response.
    /// </returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Consumes("application/json")]
    public async Task<IActionResult> UploadProfileImage([FromBody] ProfileImageUploadDto dto)
    {
        var (success, errorMessage, imageUrl) = await _userProfileService.UploadProfileImageAsync(dto, User);

        if (!success)
        {
            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                TempData["ErrorMessage"] = errorMessage;
                ModelState.AddModelError(string.Empty, errorMessage);
            }

            // Determine appropriate HTTP response code based on error type
            if (errorMessage == ImageUploadMessages.NoImageDataProvided ||
                errorMessage == ImageUploadMessages.InvalidImageDataFormat ||
                errorMessage == ImageUploadMessages.InvalidBase64Data ||
                errorMessage?.StartsWith(ImageUploadMessages.FileSizeExceedsPrefix) == true)
            {
                return BadRequest(new { success = false, message = errorMessage });
            }

            if (errorMessage == ImageUploadMessages.UserNotIdentified)
            {
                return Unauthorized(new { success = false, message = errorMessage });
            }

            return StatusCode(500, new { success = false, message = errorMessage ?? ImageUploadMessages.UploadError });
        }

        TempData["SuccessMessage"] = ImageUploadMessages.UploadSuccess;

        string displayUrl = await _blobStorageService.GetReadUrlAsync(
            imageUrl!,
            TimeSpan.FromHours(1));

        return Ok(new
        {
            success = true,
            imageUrl,
            displayUrl,
            message = ImageUploadMessages.UploadSuccess
        });
    }

    /// <summary>
    /// Determines whether the current signed-in user is allowed to self-delete.
    /// </summary>
    private bool CanCurrentUserSelfDelete()
    {
        bool hasBlockedRole = User.IsInRole("Administrator") || User.IsInRole("Manager");

        if (hasBlockedRole)
        {
            return false;
        }

        return User.IsInRole("Registered_Visitor") || User.IsInRole("Free_Counselor") || User.IsInRole("Paid_Counselor");
    }
}