using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using TeamYellow.DTOs;
using TeamYellow.Repositories;

namespace TeamYellow.Services;

public class CounsellorService
{
    private readonly ICounsellorRepository _repository;
    private readonly UserManager<IdentityUser> _userManager;

    public CounsellorService(
        ICounsellorRepository repository,
        UserManager<IdentityUser> userManager)
    {
        _repository = repository;
        _userManager = userManager;
    }

    public async Task<CounsellorDashboardDto> GetCounsellorDashboardAsync(ClaimsPrincipal user)
    {
        string userId = _userManager.GetUserId(user);
        CounsellorDashboardDto dto = await _repository.GetCounsellorDashboardDtoAsync(userId);

        return dto;
    }
}