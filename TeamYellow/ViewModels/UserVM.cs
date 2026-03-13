using System.ComponentModel.DataAnnotations;

namespace TeamYellow.ViewModels
{
    /// <summary>
    /// ViewModel for displaying user information in lists or dropdowns
    /// </summary>
    public class UserVM
    {
        [Required]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;
    }
}
