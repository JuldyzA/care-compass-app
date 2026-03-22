using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using TeamYellow.Models;

namespace TeamYellow.ViewModels
{
    public class ApplyDiscountVM
    {
        [Range(1, int.MaxValue, ErrorMessage = "Please select a discount.")]
        public int DiscountId { get; set; }

        [MinLength(1, ErrorMessage = "Please select at least one plan.")]
        public List<int> PlanIds { get; set; } = new();

        public IEnumerable<Plan>? Plans { get; set; }
        public IEnumerable<SelectListItem>? DiscountCodeOptions { get; set; }
    }
}