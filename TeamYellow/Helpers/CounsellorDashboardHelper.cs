using TeamYellow.DTOs;
using TeamYellow.ViewModels;

namespace TeamYellow.Helpers;

public static class CounsellorDashboardHelper
{
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
            PlanCreatedAt = dto.PlanCreatedAt
        };
    }
}
