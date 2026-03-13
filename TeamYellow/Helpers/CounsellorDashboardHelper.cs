using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using TeamYellow.DTOs;
using TeamYellow.Models;
using TeamYellow.ViewModels;

namespace TeamYellow.Helpers;

public static class CounsellorDashboardHelper
{
    public static CounsellorDashboardDto MapToDashboardDto(
        Counsellor counsellor,
        UserProfile profile,
        Subscription? sub,
        List<Client> client
    ) {
        var (monthlyCounts, activeCount, inActiveCount) = CalculateMonthlyCounts(client);

        var dto = new CounsellorDashboardDto
        {
            // UserProfile data
            ProfilePhotoUrl = profile.ProfilePhotoUrl,

            // Counsellor data
            DisplayName = counsellor.DisplayName,

            // Subscription data
            Status = sub?.Status ?? SubscriptionStatus.Expired,
            CycleStart = sub?.CycleStart ?? DateTime.MinValue,
            CycleEnd = sub?.CycleEnd ?? DateTime.MinValue,

            // Post Query data processing
            MonthlyClientCounts = monthlyCounts,
            ActiveClientCount = activeCount,
            InActiveClientCount = inActiveCount
        };

        return dto;
    }

    public static CounsellorDashboardVM MapToVm(CounsellorDashboardDto? dto, string? userId, string? email)
    {
        if (dto == null || userId == null || email == null)
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
            InActiveClientCount = dto.InActiveClientCount,
            TotalSubscriptionDays = dto.IsSubscriptionActive ? (int)(dto.CycleEnd - dto.CycleStart).TotalDays : 0,
            RemainingSubscriptionDays = dto.IsSubscriptionActive ? (int)(dto.CycleEnd - DateTime.Today).TotalDays : 0
        };
    }

    public static ClientTableVm MapToVm(ClientTableDto dto)
    {
        List<ClientVM> clientVMs = dto.Clients.Select(c =>
            new ClientVM
            {
                FirstName = c.FirstName,
                LastName = c.LastName,
                Initials = $"{c.FirstName?[0]}{c.LastName?[0]}",
                Email = c.Email,
                Phone = c.Phone,
                Status = c.Status == ClientStatus.Active ? true : false,
                CreatedAt = c.CreatedAt
            }).ToList();

        return new ClientTableVm
        {
            Page = dto.Page,
            StartEntry = (dto.Page - 1) * dto.PageSize + 1,
            EndEntry = dto.Page * dto.PageSize,
            TotalCount = dto.TotalCount,
            Clients = clientVMs
        };
    }

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

    private static double CalculateGrowth(int lastMonth, int currentMonth)
    {
        if (lastMonth == 0)
        {
            return currentMonth > 0 ? 100.0 : 0.0;
        }
        return Math.Round(((double)(currentMonth - lastMonth) / lastMonth) * 100, 2);
    }
}
