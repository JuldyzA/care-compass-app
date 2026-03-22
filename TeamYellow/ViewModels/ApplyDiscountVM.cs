using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using TeamYellow.Models;

namespace TeamYellow.ViewModels
{
    public class ApplyDiscountVM : IValidatableObject
    {
        [Range(1, int.MaxValue, ErrorMessage = "Please select a discount.")]
        public int DiscountId { get; set; }

        public List<int> PlanIds { get; set; } = new();

        public IEnumerable<Plan>? Plans { get; set; }
        public IEnumerable<SelectListItem>? DiscountCodeOptions { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (PlanIds == null || PlanIds.Count < 1)
            {
                yield return new ValidationResult(
                    "Please select at least one plan.",
                    new[] { nameof(PlanIds) }
                );
            }
        }
    }
}