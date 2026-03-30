using System.ComponentModel.DataAnnotations;

namespace TeamYellow.ViewModels;

/// <summary>
/// View model representing user profile information for display and editing.
/// </summary>
public class UserProfileVM
{
    [Display(Name = "Profile ID")]
    public int UserProfileId { get; set; }

    [Required]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 50 characters.")]
    [RegularExpression(@"^[A-Za-zÀ-ÖØ-öø-ÿ''\-\s]+$", ErrorMessage = "First name can contain letters, spaces, hyphens and apostrophes only.")]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 50  characters.")]
    [RegularExpression(@"^[A-Za-zÀ-ÖØ-öø-ÿ''\-\s]+$", ErrorMessage = "Last name can contain letters, spaces, hyphens and apostrophes only.")]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Invalid phone format")]
    [RegularExpression(@"^\+?[0-9\-\s\(\)]{7,20}$", ErrorMessage = "Phone must be 7-20 digits and may include '+', spaces, dashes or parentheses.")]
    [StringLength(20, MinimumLength = 7, ErrorMessage = "Phone must be between 7 and 20 characters.")]
    [Display(Name = "Phone Number")]
    public string? Phone { get; set; }

    [StringLength(80, ErrorMessage = "City cannot exceed 80 characters.")]
    [RegularExpression(@"^[A-Za-z\s\-'.]+$", ErrorMessage = "City can only contain letters, spaces, hyphens, and apostrophes.")]
    public string? City { get; set; }

    [StringLength(2, MinimumLength = 2, ErrorMessage = "Province must be a 2-character code.")]
    [RegularExpression(@"^(AB|BC|MB|NB|NL|NS|NT|NU|ON|PE|QC|SK|YT)$", ErrorMessage = "Invalid province code.")]
    public string? Province { get; set; }

    [StringLength(7, ErrorMessage = "Postal code cannot exceed 7 characters.")]
    [RegularExpression(@"^[A-Za-z]\d[A-Za-z][ -]?\d[A-Za-z]\d$", ErrorMessage = "Invalid postal code format (e.g., V5K 0A1).")]
    [Display(Name = "Postal Code")]
    public string? PostalCode { get; set; }

    [StringLength(120, ErrorMessage = "Street address cannot exceed 120 characters.")]
    [Display(Name = "Street Address")]
    public string? Street { get; set; }

    [Range(1, 9999999, ErrorMessage = "Unit number must be between 1 and 9999999.")]
    [Display(Name = "Unit Number")]
    public int? UnitNumber { get; set; }

    [Url(ErrorMessage = "Invalid URL format.")]
    [Display(Name = "Profile Photo URL")]
    public string? ProfilePhotoUrl { get; set; }

    [Display(Name = "Created At")]
    public DateTime CreatedAt { get; set; }

    [Display(Name = "Updated At")]
    public DateTime? UpdatedAt { get; set; }

    [EmailAddress(ErrorMessage = "Invalid Email Address")]
    [Display(Name = "Email Address")]
    public string? Email { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();
}