using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using TeamYellow.DTOs;
using TeamYellow.Helpers;
using TeamYellow.Models;
using TeamYellow.Repositories;
using TeamYellow.ViewModels;

namespace TeamYellow.Services;

public class CounsellorService
{
    private readonly CounsellorRepository _repository;
    private readonly UserManager<IdentityUser> _userManager;

    public CounsellorService(
        CounsellorRepository repository,
        UserManager<IdentityUser> userManager)
    {
        _repository = repository;
        _userManager = userManager;
    }

    public async Task<CounsellorDashboardVM> GetCounsellorDashboardAsync(ClaimsPrincipal user)
    {
        string? userId = _userManager.GetUserId(user);
        CounsellorDashboardDto? dto = await _repository.GetCounsellorDashboardDtoAsync(userId);

        if (dto != null)
        {
            dto.IsSubscriptionActive = dto.CycleEnd > DateTime.UtcNow && dto.Status == SubscriptionStatus.Active;
        }

        CounsellorDashboardVM vm = CounsellorDashboardHelper.MapToVm(dto, userId, user.Identity?.Name);

        return vm;
    }

    public async Task<ClientTableVm> GetClientsAsync(ClaimsPrincipal user, int page, int pageSize)
    {
        string? userId = _userManager.GetUserId(user);
        ClientTableDto dto = await _repository.GetClientsAsync(userId, page, pageSize);

        ClientTableVm clientTableVm = CounsellorDashboardHelper.MapToVm(dto);

        return clientTableVm;
    }
}