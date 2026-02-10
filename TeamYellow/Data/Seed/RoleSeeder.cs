using Microsoft.AspNetCore.Identity;

namespace TeamYellow.Data.Seed;

public class RoleSeeder : IDataSeeder
{
    private readonly RoleManager<IdentityRole> _roleManager;

    public RoleSeeder(RoleManager<IdentityRole> roleManager)
    {
        _roleManager = roleManager;
    }

    public async Task SeedAsync()
    {
        string[] roles =
        {
            "Administrator",
            "Manager",
            "Paid_Counselor",
            "Free_Counselor",
            "Registered_Visitor"
        };

        foreach (var role in roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }
}
