using Microsoft.EntityFrameworkCore;
using TeamYellow.Models;

namespace TeamYellow.Data.Seed;

public class ClientSeeder : IDataSeeder
{
    private readonly ApplicationDbContext _db;

    public ClientSeeder(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task SeedAsync()
    {
        // Prevent seeding if table is not empty
        if (await _db.Clients.AnyAsync())
        {
            Console.WriteLine("Client table already has data. Skipping seeding.");
            return;
        }

        // Get counsellors by email
        var counsellor1 = await _db.Counsellors.Include(c => c.User)
            .FirstOrDefaultAsync(c => c.User != null && c.User.Email == "counsellor1@test.ca");

        var counsellor2 = await _db.Counsellors.Include(c => c.User)
            .FirstOrDefaultAsync(c => c.User != null && c.User.Email == "counsellor2@test.ca");

        var counsellor3 = await _db.Counsellors.Include(c => c.User)
            .FirstOrDefaultAsync(c => c.User != null && c.User.Email == "counsellor3@test.ca");

        var counsellor4 = await _db.Counsellors.Include(c => c.User)
            .FirstOrDefaultAsync(c => c.User != null && c.User.Email == "counsellor4@test.ca");

        if (counsellor1 == null || counsellor2 == null || counsellor3 == null || counsellor4 == null)
        {
            Console.WriteLine("One or more counsellors not found for client seeding.");
            return;
        }

        // Sample client data
        var firstNames = new[] { "John", "Jane", "Michael", "Emily", "Daniel", "Sarah", "David", "Laura", "Matthew", "Olivia", "James", "Chloe", "Joshua", "Sophia", "Andrew", "Grace", "Ryan", "Hannah", "Ethan", "Isabella" };
        var lastNames = new[] { "Smith", "Johnson", "Brown", "Taylor", "Wilson", "Lee", "Martin", "Clark", "Walker", "Hall", "Adams", "Baker", "Carter", "Evans", "Gonzalez", "Harris", "King", "Lewis", "Mitchell", "Perez" };

        var random = new Random();

        // Helper to create clients
        async Task AddClientsAsync(int count, Counsellor counsellor)
        {
            for (int i = 0; i < count; i++)
            {
                string firstName = firstNames[random.Next(firstNames.Length)];
                string lastName = lastNames[random.Next(lastNames.Length)];
                string email = $"{firstName.ToLower()}.{lastName.ToLower()}{random.Next(1, 1000)}@example.com";
                string phone = $"{random.Next(200, 999)}-{random.Next(100, 999)}-{random.Next(1000, 9999)}";

                if (await _db.Clients.AnyAsync(c => c.Email == email))
                    continue;

                _db.Clients.Add(new Client
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Email = email,
                    Phone = phone,
                    Status = ClientStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                    CounsellorId = counsellor.CounsellorId
                });
            }
        }

        // Seed clients
        await AddClientsAsync(20, counsellor1);
        await AddClientsAsync(20, counsellor2);
        await AddClientsAsync(10, counsellor3);
        await AddClientsAsync(5, counsellor4);

        await _db.SaveChangesAsync();
    }
}
