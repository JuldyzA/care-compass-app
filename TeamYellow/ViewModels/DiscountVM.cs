using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using TeamYellow.Models;
using System.Collections.Generic;

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
        [RegularExpression(@"^\d{1,3}(.\d{1,2})?$",
        ErrorMessage = "Discount can have up to two decimal places only.")]
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

        // PLAN SELECTION
        public IEnumerable<SelectListItem>? AvailablePlans { get; set; }
        public List<int> PlanIds { get; set; } = new();

        // dropdown helper aplly discount
        public IEnumerable<SelectListItem>? DiscountCodeOptions { get; set; }

        //edit discount
        public bool IsStarted { get; set; }
        public bool IsExpired { get; set; }
        public bool HasPlans { get; set; }
    }
}
