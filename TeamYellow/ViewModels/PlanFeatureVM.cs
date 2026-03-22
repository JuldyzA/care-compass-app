using System.ComponentModel.DataAnnotations;

namespace TeamYellow.ViewModels
{
    public class PlanFeatureVM
    {
        [Required]
        public string FeatureName { get; set; } = string.Empty;

        [Required]
        public string FeatureDescription { get; set; } = string.Empty;
    }
}
