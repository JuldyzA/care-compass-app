using TeamYellow.DTOs;
using TeamYellow.Models;
using TeamYellow.ViewModels;

namespace TeamYellow.Helpers;

public static class CounsellorDashboardHelper
{
    public static CounsellorDashboardDto MapToDashboardDto(
        Counsellor counsellor,
        UserProfile profile,
        Subscription? sub)
    {
        var (monthlyCounts, growth, status) = CalculateMonthlyCounts(counsellor.Clients);

        var dto = new CounsellorDashboardDto
        {
            // UserProfile data
            UserProfileId = profile.UserProfileId,
            FirstName = profile.FirstName,
            LastName = profile.LastName,
            Phone = profile.Phone,
            ProfileCreateAt = profile.CreatedAt,
            ProfilePhotoUrl = profile.ProfilePhotoUrl,
            UnitNumber = profile.UnitNumber,
            Street = profile.Street,
            City = profile.City,
            Province = profile.Province,
            PostalCode = profile.PostalCode,

            // Counsellor data
            CounsellerId = counsellor.CounsellorId,
            PractitionerLicenceId = counsellor.PractitionerLicenceId,
            DisplayName = counsellor.DisplayName,
            IsCounsellorActive = counsellor.IsActive,

            // Subscription data
            SubscriptionId = sub?.SubscriptionId ?? 0,
            Status = sub?.Status ?? SubscriptionStatus.Expired,
            CycleStart = sub?.CycleStart ?? DateTime.MinValue,
            CycleEnd = sub?.CycleEnd ?? DateTime.MinValue,
            UpdatedAt = sub?.UpdatedAt ?? DateTime.MinValue,

            // Plan data
            PlanId = sub?.Plan?.PlanId ?? 0,
            PlanName = sub?.Plan?.PlanName ?? "No Plan",
            PlanDescription = sub?.Plan?.PlanDescription ?? string.Empty,
            Price = sub?.Plan?.Price ?? 0m,
            BillingType = sub?.Plan?.BillingType ?? string.Empty,
            IsPlanActive = sub?.Plan?.IsActive ?? false,

            // Post Query BookKeeping
            MonthlyClientCounts = monthlyCounts,
            ClientGrowthFromLastMonth = growth,
            ActiveClientCount = status
        };

        return dto;
    }

    public static CounsellorDashboardVM? MapToVm(CounsellorDashboardDto? dto, string? userId, string? email)
    {
        if (dto == null || userId == null || email == null) return null;

        return new CounsellorDashboardVM
        {
            UserId = userId,
            Email = email,
            UserProfileId = dto.UserProfileId,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Phone = dto.Phone,
            ProfileCreateAt = dto.ProfileCreateAt,
            ProfilePhotoUrl = dto.ProfilePhotoUrl,
            UnitNumber = dto.UnitNumber,
            Street = dto.Street,
            City = dto.City,
            Province = dto.Province,
            PostalCode = dto.PostalCode,
            CounsellerId = dto.CounsellerId,
            PractitionerLicenceId = dto.PractitionerLicenceId,
            DisplayName = dto.DisplayName,
            IsCounsellorActive = dto.IsCounsellorActive,
            SubscriptionId = dto.SubscriptionId,
            Status = dto.Status,
            IsSubscriptionActive = dto.IsSubscriptionActive,
            CycleStart = dto.CycleStart,
            CycleEnd = dto.CycleEnd,
            UpdatedAt = dto.UpdatedAt,
            PlanId = dto.PlanId,
            PlanName = dto.PlanName,
            PlanDescription = dto.PlanDescription,
            Price = dto.Price,
            BillingType = dto.BillingType,
            IsPlanActive = dto.IsPlanActive,
            PlanCreatedAt = dto.PlanCreatedAt,
            MonthlyClientCounts = dto.MonthlyClientCounts,
            ClientGrowthFromLastMonth = dto.ClientGrowthFromLastMonth,
            ActiveClientCount = dto.ActiveClientCount
        };
    }

    private static (int[] monthlyCounts, double growthPercent, int[] statusSplit) CalculateMonthlyCounts(IEnumerable<Client> clients)
    {
        int[] monthlyCounts = new int[12];
        int activeCount = 0;
        int totalCount = 0;
        DateTime today = DateTime.Today;

        foreach (var client in clients)
        {
            totalCount++;

            if (client.Status == ClientStatus.Active)
            {
                activeCount++;
            }

            // Build the 12-month count array
            int monthDiff = (today.Year - client.CreatedAt.Year) * 12 + (today.Month - client.CreatedAt.Month);
            if (monthDiff >= 0 && monthDiff < 12)
            {
                monthlyCounts[11 - monthDiff]++;
            }
        }

        // Calculate Growth
        int lastMonth = monthlyCounts[10];
        int currentMonth = monthlyCounts[11];
        double growth = lastMonth == 0
            ? (currentMonth > 0 ? 100.0 : 0.0)
            : Math.Round(((double)(currentMonth - lastMonth) / lastMonth) * 100, 2);

        int[] status = new int[] { activeCount, totalCount - activeCount };

        return (monthlyCounts, growth, status);
    }
}
