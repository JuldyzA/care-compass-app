using Microsoft.EntityFrameworkCore;
using TeamYellow.Data;
using TeamYellow.Data.Seed;
using TeamYellow.Models;

public class SubscriptionSeeder : IDataSeeder
{
    private readonly ApplicationDbContext _db;

    public SubscriptionSeeder(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task SeedAsync()
    {
        if (await _db.Subscriptions.AnyAsync())
            return;

        // Get plans
        var freePlan = await _db.Plans.FirstOrDefaultAsync(p => p.PlanName == "Free");
        var monthlyPlan = await _db.Plans.FirstOrDefaultAsync(p => p.PlanName == "Monthly");
        var yearlyPlan = await _db.Plans.FirstOrDefaultAsync(p => p.PlanName == "Yearly");

        if (freePlan == null || monthlyPlan == null || yearlyPlan == null)
            return;

        // Get counsellors by email safely
        var counsellor1 = await _db.Counsellors
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.User != null && c.User.Email == "consellor1@test.ca");
        var counsellor2 = await _db.Counsellors
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.User != null && c.User.Email == "consellor2@test.ca");
        var counsellor3 = await _db.Counsellors
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.User != null && c.User.Email == "consellor3@test.ca");
        var counsellor4 = await _db.Counsellors
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.User != null && c.User.Email == "consellor4@test.ca");

        // Exit if any counsellor is not found
        if (counsellor1 == null || counsellor2 == null || counsellor3 == null || counsellor4 == null)
        {
            Console.WriteLine("One or more counsellors not found for Subscription seeding.");
            return;
        }

        var now = DateTime.UtcNow;

        var subscriptions = new List<Subscription>
        {
            // Yearly subscriptions
            new Subscription
            {
                PlanId = yearlyPlan.PlanId,
                CounsellorId = counsellor1.CounsellorId,
                Status = SubscriptionStatus.Active,
                CycleStart = now,
                CycleEnd = now.AddYears(1),
                UpdatedAt = now
            },
            new Subscription
            {
                PlanId = yearlyPlan.PlanId,
                CounsellorId = counsellor2.CounsellorId,
                Status = SubscriptionStatus.Active,
                CycleStart = now,
                CycleEnd = now.AddYears(1),
                UpdatedAt = now
            },

            // Monthly subscription
            new Subscription
            {
                PlanId = monthlyPlan.PlanId,
                CounsellorId = counsellor3.CounsellorId,
                Status = SubscriptionStatus.Active,
                CycleStart = now,
                CycleEnd = now.AddMonths(1),
                UpdatedAt = now
            },

            // Free subscription
            new Subscription
            {
                PlanId = freePlan.PlanId,
                CounsellorId = counsellor4.CounsellorId,
                Status = SubscriptionStatus.Active,
                CycleStart = now,
                CycleEnd = now.AddMonths(1), // optional: free trial period
                UpdatedAt = now
            }
        };

        _db.Subscriptions.AddRange(subscriptions);
        await _db.SaveChangesAsync();
    }
}
