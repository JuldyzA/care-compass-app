using Microsoft.AspNetCore.Identity;
using TeamYellow.Configurations;

namespace TeamYellow.Data.Seed;

public class IdentitySeeder : IDataSeeder
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly string _password;

    public IdentitySeeder(UserManager<IdentityUser> userManager, SeedConfiguration seedConfig)
    {
        _userManager = userManager;
        _password = seedConfig.DefaultPassword;
    }

    public async Task SeedAsync()
    {
        var users = new[]
        {
            new { Email = "admin@test.ca",      Role = "Administrator" },
            new { Email = "manager@test.ca",    Role = "Manager" },
            new { Email = "counsellor1@test.ca", Role = "Paid_Counselor" },
            new { Email = "counsellor2@test.ca", Role = "Paid_Counselor" },
            new { Email = "counsellor3@test.ca", Role = "Paid_Counselor" },
            new { Email = "counsellor4@test.ca", Role = "Free_Counselor" },
            new { Email = "visitor@test.ca",    Role = "Registered_Visitor" }
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

                var createResult = await _userManager.CreateAsync(user, _password);
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
    }
}