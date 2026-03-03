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

    public async Task<CounsellorDashboardDto> GetCounsellorDashboardDtoAsync(string userId)
    {
        CounsellorDashboardDto? dto = await _context.Counsellors
            .Where(c => c.UserId == userId)
            .Join(_context.UserProfiles,
                c => c.UserId,
                u => u.UserId,
                (c, u) => new
                {
                    Counsellor = c,
                    UserProfile = u
                })

            .Select(x => new
            {
                x.Counsellor,
                x.UserProfile,
                LatestSubscription = _context.Subscriptions
                    .Where(s => s.CounsellorId == x.Counsellor.CounsellorId)
                    .OrderByDescending(s => s.UpdatedAt)
                    .Select(s => new
                    {
                        s.Status,
                        s.CycleStart,
                        s.CycleEnd,
                        s.UpdatedAt,
                        Plan = _context.Plans.FirstOrDefault(p => p.PlanId == s.PlanId)
                    })
                    .FirstOrDefault()
            })
            .Select(x => new CounsellorDashboardDto
            {
                // From UserProfile
                FirstName = x.UserProfile.FirstName,
                LastName = x.UserProfile.LastName,
                Phone = x.UserProfile.Phone,
                profileCreateAt = x.UserProfile.CreatedAt,
                ProfilePhotoUrl = x.UserProfile.ProfilePhotoUrl,
                UnitNumber = x.UserProfile.UnitNumber,
                Street = x.UserProfile.Street,
                City = x.UserProfile.City,
                Province = x.UserProfile.Province,
                PostalCode = x.UserProfile.PostalCode,

                // From Counsellor
                PractitionerLicenceId = x.Counsellor.PractitionerLicenceId,
                DisplayName = x.Counsellor.DisplayName,
                IsCounsellorActive = x.Counsellor.IsActive,

                // From Subscription (with null checks)
                status = x.LatestSubscription != null ? x.LatestSubscription.Status : SubscriptionStatus.Expired,
                CycleStart = x.LatestSubscription != null ? x.LatestSubscription.CycleStart : DateTime.MinValue,
                CycleEnd = x.LatestSubscription != null ? x.LatestSubscription.CycleEnd : DateTime.MinValue,
                UpdatedAt = x.LatestSubscription != null ? x.LatestSubscription.UpdatedAt : DateTime.MinValue,

                // From Plan
                PlanName = (x.LatestSubscription != null && x.LatestSubscription.Plan != null)
                    ? x.LatestSubscription.Plan.PlanName
                    : "No Plan",
                PlanDescription = (x.LatestSubscription != null && x.LatestSubscription.Plan != null)
                    ? x.LatestSubscription.Plan.PlanDescription
                    : string.Empty,
                Price = (x.LatestSubscription != null && x.LatestSubscription.Plan != null)
                    ? x.LatestSubscription.Plan.Price
                    : 0m,
                BillingType = (x.LatestSubscription != null && x.LatestSubscription.Plan != null)
                    ? x.LatestSubscription.Plan.BillingType
                    : string.Empty,
                IsPlanActive = (x.LatestSubscription != null && x.LatestSubscription.Plan != null)
                    ? x.LatestSubscription.Plan.IsActive
                    : false,

                // From Client (List)
                Clients = _context.Clients
                    .Where(cl => cl.CounsellorId == x.Counsellor.CounsellorId)
                    .Select(cl => new ClientDto
                    {
                        FirstName = cl.FirstName,
                        LastName = cl.LastName,
                        Email = cl.Email,
                        Phone = cl.Phone,
                        Status = cl.Status,
                        CreatedAt = cl.CreatedAt
                    })
            })
            .FirstOrDefaultAsync();

        return dto;
    }
}