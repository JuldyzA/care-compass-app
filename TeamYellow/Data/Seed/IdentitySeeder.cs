using Microsoft.AspNetCore.Identity;
using TeamYellow.Configurations;

namespace TeamYellow.Data.Seed;

public class IdentitySeeder : IDataSeeder
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SeedConfiguration _seedConfig;

    public IdentitySeeder(UserManager<IdentityUser> userManager, SeedConfiguration seedConfig)
    {
        _userManager = userManager;
        _seedConfig = seedConfig;
    }

    public async Task SeedAsync()
    {
        var users = new[]
        {
            new { Email = "admin@test.ca",      Role = "Administrator", Password = _seedConfig.DefaultPassword },
            new { Email = "manager@test.ca",    Role = "Manager", Password = _seedConfig.DefaultPassword },
            new { Email = "counsellor1@test.ca", Role = "Paid_Counselor", Password = _seedConfig.DefaultPassword },
            new { Email = "counsellor2@test.ca", Role = "Paid_Counselor", Password = _seedConfig.DefaultPassword },
            new { Email = "counsellor3@test.ca", Role = "Paid_Counselor", Password = _seedConfig.DefaultPassword },
            new { Email = "counsellor4@test.ca", Role = "Free_Counselor", Password = _seedConfig.DefaultPassword },
            new { Email = "counsellordemo@test.ca", Role = "Paid_Counselor", Password = _seedConfig.DemoPassword },
            new { Email = "visitor@test.ca",    Role = "Registered_Visitor", Password = _seedConfig.DefaultPassword }
        };

        foreach (var entry in users)
        {
            var user = await _userManager.FindByEmailAsync(entry.Email);

            if (user == null)
            {
                user = new IdentityUser
                {
                    UserName = entry.Email,
                    Email = entry.Email,
                    EmailConfirmed = true
                };

                var createResult = await _userManager.CreateAsync(user, entry.Password);
                if (!createResult.Succeeded)
                {
                    throw new Exception($"Failed to create user {entry.Email}: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
                }
            }

            if (!await _userManager.IsInRoleAsync(user, entry.Role))
            {
                var roleResult = await _userManager.AddToRoleAsync(user, entry.Role);
                if (!roleResult.Succeeded)
                {
                    throw new Exception($"Failed to add user {entry.Email} to role {entry.Role}: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                }
            }
        }

        // TEMPORARY: force-reset demo counsellor password to new value.
        // Remove this block after confirming the reset took effect in production.
        var demoUser = await _userManager.FindByEmailAsync("counsellordemo@test.ca");
        if (demoUser != null)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(demoUser);
            var resetResult = await _userManager.ResetPasswordAsync(demoUser, token, _seedConfig.DemoPassword);
            if (!resetResult.Succeeded)
            {
                throw new Exception($"Failed to reset demo password: {string.Join(", ", resetResult.Errors.Select(e => e.Description))}");
            }
        }
    }
}