using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="UserController"/> class.
    /// </summary>
    /// <param name="userProfileService">Provides user profile business logic.</param>
    public UserController(UserProfileService userProfileService)
    {
        _userProfileService = userProfileService;
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
}