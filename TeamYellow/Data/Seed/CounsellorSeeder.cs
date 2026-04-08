using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TeamYellow.Models;

namespace TeamYellow.Data.Seed;

public class CounsellorSeeder : IDataSeeder
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;

    public CounsellorSeeder(ApplicationDbContext db, UserManager<IdentityUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task SeedAsync()
    {
        var profiles = new[]
        {
            new { Email = "counsellor1@test.ca", FirstName = "Ethan", LastName = "Collins" },
            new { Email = "counsellor2@test.ca", FirstName = "Olivia", LastName = "Turner" },
            new { Email = "counsellor3@test.ca", FirstName = "Liam", LastName = "Walker" },
            new { Email = "counsellor4@test.ca", FirstName = "Emma", LastName = "Morrison" }
        };

        foreach (var entry in profiles)
        {
            var user = await _userManager.FindByEmailAsync(entry.Email);
            if (user == null)
            {
                Console.WriteLine($"Skipping counsellor {entry.Email}: user not found");
                continue;
            }

            // Skip if already exists
            var existing = await _db.Counsellors.FirstOrDefaultAsync(c => c.UserId == user.Id);
            if (existing != null)
                continue;

            // Generate random PractitionerLicenceId: uppercase letter or digit + 6 digits
            var random = new Random();
            string licenceId = $"{(char)('A' + random.Next(0, 26))}{random.Next(100000, 1000000)}";

            var counsellor = new Counsellor
            {
                UserId = user.Id,
                DisplayName = $"{entry.FirstName} {entry.LastName}",
                PractitionerLicenceId = licenceId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _db.Counsellors.Add(counsellor);
        }

        await _db.SaveChangesAsync();
    }
}
