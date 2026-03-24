using TeamYellow.Models;

namespace TeamYellow.Helpers;

/// <summary>
/// Provides helper methods for mapping counsellor dashboard data between entities, DTOs, and view models.
/// </summary>
public static class DiscountCalculator
{
    /// <summary>
    /// Calculates the discount amount to apply to the original price based on the discount type and value.
    /// The returned amount is clamped between zero and the original price and rounded to two decimal places.
    /// </summary>
    /// <param name="originalPrice">The original plan price before discount.</param>
    /// <param name="discount">The discount definition to apply.</param>
    /// <returns>The calculated discount amount.</returns>
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