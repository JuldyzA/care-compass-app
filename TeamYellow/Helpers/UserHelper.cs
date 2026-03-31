using Microsoft.AspNetCore.Identity;
using TeamYellow.Models;
using TeamYellow.ViewModels;

namespace TeamYellow.Helpers;

/// <summary>
/// Provides helper methods for mapping user profile data between entities and view models.
/// </summary>
public class UserHelper
{
    /// <summary>
    /// Maps a user profile entity to its view model representation.
    /// </summary>
    /// <param name="profile">The user profile entity.</param>
    /// <param name="email">The user's email address.</param>
    /// <returns>A populated user profile view model.</returns>
    public static UserProfileVM MapToVM(UserProfile profile, string? email)
    {
        return new UserProfileVM
        {
            UserProfileId = profile.UserProfileId,
            FirstName = profile.FirstName,
            LastName = profile.LastName,
            Phone = profile.Phone,
            City = profile.City,
            Province = profile.Province,
            PostalCode = profile.PostalCode,
            Street = profile.Street,
            UnitNumber = profile.UnitNumber,
            ProfilePhotoUrl = profile.ProfilePhotoUrl,
            CreatedAt = profile.CreatedAt,
            UpdatedAt = profile.UpdatedAt,
            Email = email
        };
    }

    /// <summary>
    /// Maps an identity user to an account view model with masked credentials.
    /// </summary>
    /// <param name="identityUser">The identity user entity.</param>
    /// <returns>A populated user account view model with masked password.</returns>
    public static UserAccountVM MapToVM(IdentityUser identityUser)
    {
        return new UserAccountVM
        {
            Email = identityUser.Email ?? string.Empty
        };
    }

    /// <summary>
    /// Maps a view model to a user profile entity, updating the profile's properties.
    /// </summary>
    /// <param name="vm">The view model containing updated values.</param>
    public static void UpdateEntity(UserProfile profile, UserProfileVM vm)
    {
        profile.FirstName = vm.FirstName;
        profile.LastName = vm.LastName;
        profile.Phone = vm.Phone;
        profile.City = vm.City;
        profile.Province = vm.Province;
        profile.PostalCode = vm.PostalCode;
        profile.Street = vm.Street;
        profile.UnitNumber = vm.UnitNumber;
        profile.ProfilePhotoUrl = vm.ProfilePhotoUrl;
    }
}