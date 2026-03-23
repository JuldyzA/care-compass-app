using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using TeamYellow.Models;

namespace TeamYellow.ViewModels
{
    public class PlanVM : IValidatableObject
    {
        public int PlanId { get; set; }

        [Required(ErrorMessage = "Plan name is required.")]
        [MaxLength(80)]
        [DisplayName("Plan Name")]
        public string PlanName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Plan description is required.")]
        [MaxLength(500)]
        [DisplayName("Plan description")]
        public string PlanDescription { get; set; } = string.Empty;

        [Range(0, 10000)]
        [DisplayName("Price")]
        public decimal Price { get; set; }

        [Required]
        public string BillingType { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public IEnumerable<Plan> Plans { get; set; } = new List<Plan>();
      
        public List<PlanFeatureVM> PlanFeatures { get; set; } = [];

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Price != decimal.Round(Price, 2, MidpointRounding.AwayFromZero))
            {
                yield return new ValidationResult(
                    "Price can have at most 2 decimal places.",
                    new[] { nameof(Price) });
            }
        }
    }
}
