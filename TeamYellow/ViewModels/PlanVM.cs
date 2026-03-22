using System.ComponentModel.DataAnnotations;
using TeamYellow.Models;

namespace TeamYellow.ViewModels
{
    public class PlanVM : IValidatableObject
    {
        public int PlanId { get; set; }

        [Required]
        [MaxLength(80)]
        public string PlanName { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string PlanDescription { get; set; } = string.Empty;

        [Range(0, 10000)]
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
