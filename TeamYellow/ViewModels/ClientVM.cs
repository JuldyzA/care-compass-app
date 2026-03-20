using System.ComponentModel.DataAnnotations;

namespace TeamYellow.ViewModels;

public class ClientVM
{
    public int ClientId { get; set; }

    [Required]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 50 characters.")]
    [RegularExpression(@"^[A-Za-zÀ-ÖØ-öø-ÿ'’\-\s]+$", ErrorMessage = "First name can contain letters, spaces, hyphens and apostrophes only.")]
    public string FirstName { get; set; } = null!;

    [Required]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 50 characters.")]
    [RegularExpression(@"^[A-Za-zÀ-ÖØ-öø-ÿ'’\-\s]+$", ErrorMessage = "Last name can contain letters, spaces, hyphens and apostrophes only.")]
    public string LastName { get; set; } = null!;

    [Required]
    [StringLength(5, MinimumLength = 1, ErrorMessage = "Initials must be 1 to 5 characters.")]
    [RegularExpression(@"^[A-Z]{1,5}$", ErrorMessage = "Initials must be uppercase letters without spaces.")]
    public string Initials { get; set; } = null!;

    [Required]
    [EmailAddress(ErrorMessage = "Invalid Email Address")]
    [StringLength(255, MinimumLength = 5, ErrorMessage = "Email must be between 5 and 255 characters.")]
    public string Email { get; set; } = null!;

    [Required]
    [Phone(ErrorMessage = "Invalid phone format")]
    [RegularExpression(@"^\+?[0-9\-\s\(\)]{7,20}$", ErrorMessage = "Phone must be 7-20 digits and may include '+', spaces, dashes or parentheses.")]
    [StringLength(20, MinimumLength = 7, ErrorMessage = "Phone must be between 7 and 20 characters.")]
    public string Phone { get; set; } = null!;

    public bool Status { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime CreatedAt { get; set; }
}
