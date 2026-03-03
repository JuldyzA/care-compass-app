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
        CounsellorDashboardDto? dto = await _context.Counsellors
            .Where(c => c.UserId == userId)
            .Include(c => c.Subscriptions.OrderByDescending(s => s.UpdatedAt).Take(1))
                .ThenInclude(s => s.Plan)
            .Include(c => c.Clients)
            .Join(_context.UserProfiles,
                c => c.UserId,
                u => u.UserId,
                (counsellor, userProfile) => new
                {
                    Counsellor = counsellor,
                    UserProfile = userProfile,
                    LatestSubscription = counsellor.Subscriptions.FirstOrDefault()
                })
            .Select(data => new CounsellorDashboardDto
            {
                // UserProfile data
                FirstName = data.UserProfile.FirstName,
                LastName = data.UserProfile.LastName,
                Phone = data.UserProfile.Phone,
                profileCreateAt = data.UserProfile.CreatedAt,
                ProfilePhotoUrl = data.UserProfile.ProfilePhotoUrl,
                UnitNumber = data.UserProfile.UnitNumber,
                Street = data.UserProfile.Street,
                City = data.UserProfile.City,
                Province = data.UserProfile.Province,
                PostalCode = data.UserProfile.PostalCode,

                // Counsellor data
                PractitionerLicenceId = data.Counsellor.PractitionerLicenceId,
                DisplayName = data.Counsellor.DisplayName,
                IsCounsellorActive = data.Counsellor.IsActive,

                // Subscription data
                status = data.LatestSubscription != null 
                    ? data.LatestSubscription.Status 
                    : SubscriptionStatus.Expired,
                CycleStart = data.LatestSubscription != null 
                    ? data.LatestSubscription.CycleStart 
                    : DateTime.MinValue,
                CycleEnd = data.LatestSubscription != null 
                    ? data.LatestSubscription.CycleEnd 
                    : DateTime.MinValue,
                UpdatedAt = data.LatestSubscription != null 
                    ? data.LatestSubscription.UpdatedAt 
                    : DateTime.MinValue,

                // Plan data
                PlanName = data.LatestSubscription != null && data.LatestSubscription.Plan != null
                    ? data.LatestSubscription.Plan.PlanName
                    : "No Plan",
                PlanDescription = data.LatestSubscription != null && data.LatestSubscription.Plan != null
                    ? data.LatestSubscription.Plan.PlanDescription
                    : string.Empty,
                Price = data.LatestSubscription != null && data.LatestSubscription.Plan != null
                    ? data.LatestSubscription.Plan.Price
                    : 0m,
                BillingType = data.LatestSubscription != null && data.LatestSubscription.Plan != null
                    ? data.LatestSubscription.Plan.BillingType
                    : string.Empty,
                IsPlanActive = data.LatestSubscription != null && data.LatestSubscription.Plan != null
                    ? data.LatestSubscription.Plan.IsActive
                    : false,

                // Clients collection (with navigation property)
                Clients = data.Counsellor.Clients.Select(client => new ClientDto
                {
                    FirstName = client.FirstName,
                    LastName = client.LastName,
                    Email = client.Email,
                    Phone = client.Phone,
                    Status = client.Status,
                    CreatedAt = client.CreatedAt
                })
            })
            .FirstOrDefaultAsync();

        return dto;
    }
}