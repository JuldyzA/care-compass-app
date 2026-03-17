using TeamYellow.DTOs;
using TeamYellow.Models;
using TeamYellow.ViewModels;

namespace TeamYellow.Helpers;

public class CounsellorDashboardHelper
{
    /// <summary>
    /// Maps counsellor, profile, and subscription data into a dashboard DTO, 
    /// including calculated client statistics for the current month and status totals.
    /// </summary>
    /// <param name="counsellor">The counsellor entity containing basic info.</param>
    /// <param name="profile">The user profile for photo and display details.</param>
    /// <param name="sub">The current or most recent subscription record.</param>
    /// <param name="clients">A list of all clients used to calculate dashboard metrics.</param>
    /// <returns>A DTO containing summarized dashboard data and processed client counts.</returns>
    public static CounsellorDashboardDto MapToDashboardDto(
        Counsellor counsellor,
        UserProfile? profile,
        Subscription? sub,
        List<Client> clients
    ) {
        var (monthlyCounts, activeCount, inActiveCount) = CalculateMonthlyCounts(clients);

        var dto = new CounsellorDashboardDto
        {
            ProfilePhotoUrl = profile?.ProfilePhotoUrl,
            DisplayName = counsellor.DisplayName,
            Status = sub?.Status ?? SubscriptionStatus.Expired,
            CycleStart = sub?.CycleStart ?? DateTime.MinValue,
            CycleEnd = sub?.CycleEnd ?? DateTime.MinValue,
            MonthlyClientCounts = monthlyCounts,
            ActiveClientCount = activeCount,
            InactiveClientCount = inActiveCount
        };

        return dto;
    }

    /// <summary>
    /// Converts a counsellor dashboard DTO into a View Model, calculating growth percentages 
    /// and remaining subscription duration for the UI.
    /// </summary>
    /// <param name="dto">The source data transfer object containing raw statistics.</param>
    /// <param name="userId">The unique identifier of the user to validate the mapping context.</param>
    /// <returns>A populated View Model for the dashboard or an empty instance if the DTO or userId is null.</returns>
    public static CounsellorDashboardVM MapToVm(CounsellorDashboardDto? dto, string? userId)
    {
        if (dto == null || userId == null)
        {
            return new CounsellorDashboardVM();
        }

        return new CounsellorDashboardVM
        {
            ProfilePhotoUrl = dto.ProfilePhotoUrl,
            DisplayName = dto.DisplayName,
            Status = dto.Status,
            IsSubscriptionActive = dto.IsSubscriptionActive,
            CycleStart = dto.CycleStart,
            CycleEnd = dto.CycleEnd,
            MonthlyClientCounts = dto.MonthlyClientCounts,
            ClientGrowthFromLastMonth = CalculateGrowth(dto.MonthlyClientCounts[10], dto.MonthlyClientCounts[11]),
            ActiveClientCount = dto.ActiveClientCount,
            InactiveClientCount = dto.InactiveClientCount,
            TotalSubscriptionDays = dto.IsSubscriptionActive ? (int)(dto.CycleEnd.Date - dto.CycleStart.Date).TotalDays : 0,
            RemainingSubscriptionDays = dto.IsSubscriptionActive ? (int)(dto.CycleEnd.Date - DateTime.UtcNow.Date).TotalDays : 0
        };
    }

    /// <summary>
    /// Calculates monthly client registration trends over the last 12 months and 
    /// aggregates active versus inactive client totals.
    /// </summary>
    /// <param name="clients">The collection of client entities to analyze.</param>
    /// <returns>A tuple containing an array of monthly counts and the total counts for active and inactive clients.</returns>
    private static (int[] monthlyCounts, int activeCount, int inActiveCount) CalculateMonthlyCounts(IEnumerable<Client> clients)
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

        return (monthlyCounts, activeCount, totalCount - activeCount);
    }

    /// <summary>
    /// Calculates the percentage growth between two monthly totals, handling cases where the 
    /// previous month had zero entries to avoid division by zero.
    /// </summary>
    /// <param name="lastMonth">The total count from the previous month.</param>
    /// <param name="currentMonth">The total count from the current month.</param>
    /// <returns>The growth percentage rounded to two decimal places.</returns>
    private static double CalculateGrowth(int lastMonth, int currentMonth)
    {
        if (lastMonth == 0)
        {
            return currentMonth > 0 ? 100.0 : 0.0;
        }
        return Math.Round(((double)(currentMonth - lastMonth) / lastMonth) * 100, 2);
    }
}
