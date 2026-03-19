using TeamYellow.Models;

namespace TeamYellow.Helpers;

public static class DiscountCalculator
{
    public static decimal CalculateDiscountAmount(decimal originalPrice, Discount discount)
    {
        decimal discountAmount = discount.DiscountType == DiscountType.Percent
            ? originalPrice * (discount.Value / 100m)
            : discount.Value;

        if (discountAmount < 0)
            discountAmount = 0;

        if (discountAmount > originalPrice)
            discountAmount = originalPrice;

        return decimal.Round(discountAmount, 2, MidpointRounding.AwayFromZero);
    }
}