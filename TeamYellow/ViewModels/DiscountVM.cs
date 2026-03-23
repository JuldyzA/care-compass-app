using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using TeamYellow.Models;

namespace TeamYellow.ViewModels
{
    public class DiscountVM : IValidatableObject
    {
        public int DiscountId { get; set; }

        [DisplayName("Discount Code")]
        [Required(ErrorMessage = "Discount code is required.")]
        [RegularExpression(@"^[A-Z0-9]{3,40}$",
            ErrorMessage = "Discount code must be 3–40 uppercase letters or numbers.")]
        public string DiscountCode { get; set; } = string.Empty;

        [DisplayName("Discount Type")]
        [Required(ErrorMessage = "Discount type is required.")]
        public DiscountType DiscountType { get; set; }

        [DisplayName("Discount Value")]
        [Required(ErrorMessage = "Discount value is required.")]
        public decimal? Value { get; set; }

        [DisplayName("Start Date")]
        [Required(ErrorMessage = "Start date is required.")]
        public DateTime StartDateTime { get; set; }

        [DisplayName("End Date")]
        [Required(ErrorMessage = "End date is required.")]
        public DateTime EndDateTime { get; set; }

        public DateTime CreatedAt { get; set; }

        public IEnumerable<Discount>? Discounts { get; set; }
        public IEnumerable<Plan>? Plans { get; set; }
        public IEnumerable<SelectListItem>? AvailablePlans { get; set; }
        public List<int> PlanIds { get; set; } = new();
        public IEnumerable<SelectListItem>? DiscountCodeOptions { get; set; }
        public bool IsStarted { get; set; }
        public bool IsExpired { get; set; }
        public bool HasPlans { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var isStartDateDefault = StartDateTime == default;
            var isEndDateDefault = EndDateTime == default;

            if (isStartDateDefault)
            {
                yield return new ValidationResult(
                    "Start date is required.",
                    new[] { nameof(StartDateTime) });
            }

            if (isEndDateDefault)
            {
                yield return new ValidationResult(
                    "End date is required.",
                    new[] { nameof(EndDateTime) });
            }

            if (!isStartDateDefault && !isEndDateDefault && EndDateTime <= StartDateTime)
            {
                yield return new ValidationResult(
                    "End date/time must be after start date/time.",
                    new[] { nameof(EndDateTime) });
            }

            if (!Value.HasValue)
                yield break;

            if (DiscountType == DiscountType.Percent)
            {
                if (Value.Value < 1 || Value.Value > 100)
                {
                    yield return new ValidationResult(
                        "Percentage discount must be between 1 and 100.",
                        new[] { nameof(Value) });
                }

                if (Value.Value != Math.Truncate(Value.Value))
                {
                    yield return new ValidationResult(
                        "Percentage discount must be a whole number.",
                        new[] { nameof(Value) });
                }
            }

            if (DiscountType == DiscountType.Amount)
            {
                if (Value.Value <= 0 || Value.Value > 999.99m)
                {
                    yield return new ValidationResult(
                        "Amount discount must be greater than 0 and at most 999.99.",
                        new[] { nameof(Value) });
                }

                var rounded = decimal.Round(Value.Value, 2, MidpointRounding.AwayFromZero);
                if (Value.Value != rounded)
                {
                    yield return new ValidationResult(
                        "Amount discount can have at most 2 decimal places.",
                        new[] { nameof(Value) });
                }
            }
        }
    }
}
