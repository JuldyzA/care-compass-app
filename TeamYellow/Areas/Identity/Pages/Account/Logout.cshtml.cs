// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using TeamYellow.Models;
using TeamYellow.Repositories;

namespace TeamYellow.Areas.Identity.Pages.Account
{
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<LogoutModel> _logger;
        private readonly UserLogRepository _userLogRepository;

        public LogoutModel(SignInManager<IdentityUser> signInManager, ILogger<LogoutModel> logger, UserLogRepository userLogRepository)
        {
            _signInManager = signInManager;
            _logger = logger;
            _userLogRepository = userLogRepository;
        }

        public async Task<IActionResult> OnPost(string returnUrl = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!string.IsNullOrWhiteSpace(userId))
            {
                UserLog active = await _userLogRepository.GetActiveLogAsync(userId);
                if (active != null)
                {
                    await _userLogRepository.EndLogAsync(active.LogId);
                }
            }

            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            if (returnUrl != null)
            {
                return LocalRedirect(returnUrl);
            }
            else
            {
                return RedirectToPage();
            }
        }
    }
}
