using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TeamYellow.Models;

namespace TeamYellow.Data.Seed;

public class UserLogSeeder : IDataSeeder
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;

    public UserLogSeeder(ApplicationDbContext db, UserManager<IdentityUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task SeedAsync()
    {
        var emails = new[]
        {
            "admin@test.ca",
            "manager@test.ca",
            "consellor1@test.ca",
            "consellor2@test.ca",
            "consellor3@test.ca",
            "consellor4@test.ca",
            "visitor@test.ca"
        };

        foreach (var email in emails)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) continue;

            var hasExistingLogs = await _db.UserLogs.AnyAsync(l => l.UserId == user.Id);
            if (hasExistingLogs)
                continue;

            // Create 2 new logs
            var logs = new List<UserLog>();
            for (int i = 0; i < 2; i++)
            {
                var logInTime = DateTime.UtcNow.AddDays(-i - 1).AddHours(8);
                var logOutTime = logInTime.AddHours(1 + i);
                logs.Add(new UserLog
                {
                    UserId = user.Id,
                    LogInTime = logInTime,
                    LogOutTime = logOutTime,
                    Abandoned = false
                });
            }
            _db.UserLogs.AddRange(logs);
        }

        await _db.SaveChangesAsync();
    }
}
