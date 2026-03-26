using System.ComponentModel.DataAnnotations;

namespace TeamYellow.ViewModels
{
    /// <summary>
    /// View model for displaying user information in lists and dropdowns.
    /// </summary>
    public class UserVM
    {
        public string UserId { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;
    }
}
