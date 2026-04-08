using System.ComponentModel.DataAnnotations;

namespace TeamYellow.ViewModels
{
    /// <summary>
    /// View model representing a mapping between a user and a role.
    /// </summary>
    public class UserRoleVM
    {
        [Required]
        [Display(Name = "User Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Role Name")]
        public string RoleName { get; set; } = string.Empty;
    }
}
