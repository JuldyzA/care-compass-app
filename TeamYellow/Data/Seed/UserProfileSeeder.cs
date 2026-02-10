using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TeamYellow.Models;

namespace TeamYellow.Data.Seed;

public class UserProfileSeeder : IDataSeeder
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;

    public UserProfileSeeder(ApplicationDbContext db, UserManager<IdentityUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task SeedAsync()
    {
        var profiles = new[]
        {
            new
            {
                Email = "admin@test.ca",
                FirstName = "Alexander",
                LastName = "Harrison",
                Phone = "604-555-0123",
                City = "Vancouver",
                Province = "BC",
                PostalCode = "V5K 0A1",
                Street = "123 Burrard St",
                UnitNumber = 101,
                ProfilePhotoUrl = "https://randomuser.me/api/portraits/med/men/1.jpg"
            },
            new
            {
                Email = "manager@test.ca",
                FirstName = "Samantha",
                LastName = "Reed",
                Phone = "416-555-0456",
                City = "Toronto",
                Province = "ON",
                PostalCode = "M5H 2N2",
                Street = "456 King St W",
                UnitNumber = 202,
                ProfilePhotoUrl = "https://randomuser.me/api/portraits/med/women/2.jpg"
            },
            new
            {
                Email = "consellor1@test.ca",
                FirstName = "Ethan",
                LastName = "Collins",
                Phone = "514-555-0789",
                City = "Montreal",
                Province = "QC",
                PostalCode = "H2X 1Y4",
                Street = "789 Saint Catherine St",
                UnitNumber = 303,
                ProfilePhotoUrl = "https://randomuser.me/api/portraits/med/men/3.jpg"
            },
            new
            {
                Email = "consellor2@test.ca",
                FirstName = "Olivia",
                LastName = "Turner",
                Phone = "403-555-0912",
                City = "Calgary",
                Province = "AB",
                PostalCode = "T2P 3G5",
                Street = "321 8th Ave SW",
                UnitNumber = 404,
                ProfilePhotoUrl = "https://randomuser.me/api/portraits/med/women/4.jpg"
            },
            new
            {
                Email = "consellor3@test.ca",
                FirstName = "Liam",
                LastName = "Walker",
                Phone = "613-555-0345",
                City = "Ottawa",
                Province = "ON",
                PostalCode = "K1A 0B1",
                Street = "654 Wellington St",
                UnitNumber = 505,
                ProfilePhotoUrl = "https://randomuser.me/api/portraits/med/men/5.jpg"
            },
            new
            {
                Email = "consellor4@test.ca",
                FirstName = "Emma",
                LastName = "Morrison",
                Phone = "204-555-0678",
                City = "Winnipeg",
                Province = "MB",
                PostalCode = "R3C 4T3",
                Street = "987 Portage Ave",
                UnitNumber = 606,
                ProfilePhotoUrl = "https://randomuser.me/api/portraits/med/women/6.jpg"
            },
            new
            {
                Email = "visiter@test.ca",
                FirstName = "Noah",
                LastName = "Foster",
                Phone = "902-555-0123",
                City = "Halifax",
                Province = "NS",
                PostalCode = "B3H 1A1",
                Street = "111 Spring Garden Rd",
                UnitNumber = 707,
                ProfilePhotoUrl = "https://randomuser.me/api/portraits/med/men/7.jpg"
            }
        };

        foreach (var entry in profiles)
        {
            var user = await _userManager.FindByEmailAsync(entry.Email);
            if (user == null)
            {
                Console.WriteLine($"Skipping profile for {entry.Email}: user not found");
                continue;
            }

            var existing = await _db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (existing != null)
            {
                // Update existing profile fields
                existing.FirstName = entry.FirstName;
                existing.LastName = entry.LastName;
                existing.Phone = entry.Phone;
                existing.City = entry.City;
                existing.Province = entry.Province;
                existing.PostalCode = entry.PostalCode;
                existing.Street = entry.Street;
                existing.UnitNumber = entry.UnitNumber;
                existing.ProfilePhotoUrl = entry.ProfilePhotoUrl;
                existing.UpdatedAt = DateTime.UtcNow;

                _db.UserProfiles.Update(existing);
            }
            else
            {
                var profile = new UserProfile
                {
                    FirstName = entry.FirstName,
                    LastName = entry.LastName,
                    Phone = entry.Phone,
                    City = entry.City,
                    Province = entry.Province,
                    PostalCode = entry.PostalCode,
                    Street = entry.Street,
                    UnitNumber = entry.UnitNumber,
                    ProfilePhotoUrl = entry.ProfilePhotoUrl,
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow
                };

                _db.UserProfiles.Add(profile);
            }
        }

        await _db.SaveChangesAsync();
    }
}