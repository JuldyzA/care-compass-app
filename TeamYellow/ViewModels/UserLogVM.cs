using System.ComponentModel.DataAnnotations;

namespace TeamYellow.ViewModels
{
    /// <summary>
    /// ViewModel for displaying user session log information in the UI.
    /// </summary>
    public class UserLogVM
    {
        [Required]
        [Display(Name = "User Email")]
        public string? Email { get; set; }
        
        [Display(Name = "Log In Time")] 
        public DateTime LogInTime { get; set; }

        [Display(Name = "Log Out Time")] 
        public DateTime? LogOutTime { get; set; }

        [Display(Name = "Abandoned")] 
        public bool Abandoned { get; set; }
    }
}
