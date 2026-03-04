using TeamYellow.Models;

namespace TeamYellow.ViewModels
{
    public class DiscountVM
    {
        public int DiscountId { get; set; }
        public string? DiscountCode { get; set; }
        public DiscountType DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
