using System.ComponentModel.DataAnnotations;

namespace TeamYellow.ViewModels
{
    /// <summary>
    /// View model for displaying user session log information in the user interface.
    /// </summary>
    public class UserLogVM
    {
        public int LogId { get; set; }

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
