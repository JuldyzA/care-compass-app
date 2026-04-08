using System.ComponentModel.DataAnnotations;

namespace TeamYellow.ViewModels
{
    /// <summary>
    /// View model for the delete account confirmation page.
    /// </summary>
    public class DeleteAccountVM
    {
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Type DELETE to confirm.")]
        public string ConfirmationText { get; set; } = string.Empty;
    }
}
