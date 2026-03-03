using Microsoft.EntityFrameworkCore;
using TeamYellow.Data;
using TeamYellow.DTOs;
using TeamYellow.Models;

namespace TeamYellow.Repositories;

public interface ICounsellorRepository
{
    Task<CounsellorDashboardDto?> GetCounsellorDashboardDtoAsync(string userId);
}

public class CounsellorRepository : ICounsellorRepository
{
    private readonly ApplicationDbContext _context;

    public CounsellorRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CounsellorDashboardDto?> GetCounsellorDashboardDtoAsync(string userId)
    {
        // Eagerly load Counsellor with Subscriptions (and their Plans) and Clients in one query
        Counsellor? counsellor = await _context.Counsellors
            .Where(c => c.UserId == userId)
            .Include(c => c.Subscriptions)        
                .ThenInclude(s => s.Plan)        
            .Include(c => c.Clients)             
            .FirstOrDefaultAsync();

        if (counsellor == null)
            return null;

        // Join with UserProfile to get profile details
        UserProfile? userProfile = await _context.UserProfiles
            .FirstOrDefaultAsync(u => u.UserId == counsellor.UserId);

        Subscription? latestSubscription = counsellor.Subscriptions
            .OrderByDescending(s => s.UpdatedAt)
            .FirstOrDefault();

        IReadOnlyCollection<ClientDto> clients = counsellor.Clients
            .Select(cl => new ClientDto
            {
                FirstName = cl.FirstName,
                LastName = cl.LastName,
                Email = cl.Email,
                Phone = cl.Phone,
                Status = cl.Status,
                CreatedAt = cl.CreatedAt
            })
            .ToList();

        CounsellorDashboardDto dto = new CounsellorDashboardDto
        {
            // UserProfile
            FirstName = userProfile?.FirstName ?? "Unknown",
            LastName = userProfile?.LastName ?? "Unknown",
            Phone = userProfile?.Phone,
            profileCreateAt = userProfile?.CreatedAt ?? DateTime.MinValue,
            ProfilePhotoUrl = userProfile?.ProfilePhotoUrl,
            UnitNumber = userProfile?.UnitNumber,
            Street = userProfile?.Street,
            City = userProfile?.City,
            Province = userProfile?.Province,
            PostalCode = userProfile?.PostalCode,

            // Counsellor
            PractitionerLicenceId = counsellor.PractitionerLicenceId,
            DisplayName = counsellor.DisplayName,
            IsCounsellorActive = counsellor.IsActive,

            // Subscription
            status = latestSubscription?.Status ?? SubscriptionStatus.Expired,
            CycleStart = latestSubscription?.CycleStart ?? DateTime.MinValue,
            CycleEnd = latestSubscription?.CycleEnd ?? DateTime.MinValue,
            UpdatedAt = latestSubscription?.UpdatedAt ?? DateTime.MinValue,

            // Plan
            PlanName = latestSubscription?.Plan?.PlanName ?? "No Plan",
            PlanDescription = latestSubscription?.Plan?.PlanDescription ?? string.Empty,
            Price = latestSubscription?.Plan?.Price ?? 0m,
            BillingType = latestSubscription?.Plan?.BillingType ?? string.Empty,
            IsPlanActive = latestSubscription?.Plan?.IsActive ?? false,

            // Clients
            Clients = clients
        };

        return dto;
    }
}