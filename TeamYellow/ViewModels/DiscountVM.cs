using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using TeamYellow.Models;

namespace TeamYellow.ViewModels
{
    public class DiscountVM
    {
        public int DiscountId { get; set; }

        [Required]
        [RegularExpression(@"^[A-Z0-9]{3,40}$",
            ErrorMessage = "Discount code must be 3–40 uppercase letters or numbers.")]
        public string DiscountCode { get; set; } = string.Empty;

        [Required]
        public DiscountType DiscountType { get; set; }

        [Required]
        [Range(0, 100000)]
        public decimal Value { get; set; }

        [Required]
        public DateTime StartDateTime { get; set; }

        [Required]
        public DateTime EndDateTime { get; set; }

        public DateTime CreatedAt { get; set; }

        // LIST PAGE
        public IEnumerable<Discount>? Discounts { get; set; }

        // APPLY DISCOUNT PAGE
        public IEnumerable<Plan>? Plans { get; set; }

        public List<int> PlanIds { get; set; } = new();

        // dropdown helper
        public IEnumerable<SelectListItem>? DiscountCodeOptions { get; set; }
    }
}
