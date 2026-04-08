using System.ComponentModel.DataAnnotations;

namespace TeamYellow.ViewModels
{
    /// <summary>
    /// View model representing a role used in role-related forms and views.
    /// </summary>
    public class RoleVM
    {
        [Required]
        [Display(Name = "Role Name")]
        public string RoleName { get; set; } = string.Empty;
    }
}