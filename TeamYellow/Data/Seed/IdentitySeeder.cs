using Microsoft.AspNetCore.Identity;

namespace TeamYellow.Data.Seed;

public class IdentitySeeder : IDataSeeder
{
    private readonly UserManager<IdentityUser> _userManager;

    public IdentitySeeder(UserManager<IdentityUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task SeedAsync()
    {
        var users = new[]
        {
            new { Email = "admin@test.ca",      Role = "Administrator" },
            new { Email = "manager@test.ca",    Role = "Manager" },
            new { Email = "consellor1@test.ca", Role = "Paid_Counselor" },
            new { Email = "consellor2@test.ca", Role = "Paid_Counselor" },
            new { Email = "consellor3@test.ca", Role = "Paid_Counselor" },
            new { Email = "consellor4@test.ca", Role = "Free_Counselor" },
            new { Email = "visitor@test.ca",    Role = "Registered_Visitor" }
        };

        const string password = "P@ssw0rd!";

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

                var createResult = await _userManager.CreateAsync(user, password);
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