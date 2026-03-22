using System.ComponentModel.DataAnnotations;

namespace TeamYellow.ViewModels
{
    public class PlanFeatureVM
    {
        public int PlanFeatureId { get; set; }

        [Required]
        [StringLength(120, ErrorMessage = "Feature name cannot exceed 120 characters.")]
        public string FeatureName { get; set; } = string.Empty;

        [Required]
        [StringLength(300, ErrorMessage = "Feature description cannot exceed 300 characters.")]
        public string FeatureDescription { get; set; } = string.Empty;
    }
}
