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
        [Range(typeof(decimal), "0", "999.99",
        ErrorMessage = "Discount must be between 0 and 999.99 and have up to two decimal places.")]
        public decimal Value { get; set; }

        [Required]
        public DateTime StartDateTime { get; set; }

        [Required]
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
    }
}
