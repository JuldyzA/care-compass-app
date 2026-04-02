using Microsoft.EntityFrameworkCore;
using TeamYellow.Models;

namespace TeamYellow.Data.Seed;

public class PaymentTransactionSeeder : IDataSeeder
{
    private readonly ApplicationDbContext _db;

    public PaymentTransactionSeeder(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task SeedAsync()
    {
        if (await _db.PaymentTransactions.AnyAsync())
            return;

        var now = DateTime.UtcNow;

        // Get counsellors by email safely
        var counsellor1 = await _db.Counsellors
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.User != null && c.User.Email == "counsellor1@test.ca");
        var counsellor2 = await _db.Counsellors
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.User != null && c.User.Email == "counsellor2@test.ca");
        var counsellor3 = await _db.Counsellors
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.User != null && c.User.Email == "counsellor3@test.ca");
        var counsellor4 = await _db.Counsellors
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.User != null && c.User.Email == "counsellor4@test.ca");

        if (counsellor1 == null || counsellor2 == null || counsellor3 == null || counsellor4 == null)
        {
            Console.WriteLine("One or more counsellors not found for PaymentTransaction seeding.");
            return;
        }

        // Hardcoded payer names
        var transactions = new List<PaymentTransaction>
        {
            new PaymentTransaction
            {
                PayerName = "Ethan Collins",
                Amount = 100m,
                Currency = "CAD",
                Provider = "PayPal",
                ProviderOrderId = "ORDER-1001",
                Status = PaymentTransactionStatus.Captured,
                PaidAt = now,
                SubscriptionId = 1,
                DiscountId = null
            },
            new PaymentTransaction
            {
                PayerName = "Olivia Turner",
                Amount = 100m,
                Currency = "CAD",
                Provider = "PayPal",
                ProviderOrderId = "ORDER-1002",
                Status = PaymentTransactionStatus.Captured,
                PaidAt = now,
                SubscriptionId = 2,
                DiscountId = null
            },
            new PaymentTransaction
            {
                PayerName = "Liam Walker",
                Amount = 50m,
                Currency = "CAD",
                Provider = "PayPal",
                ProviderOrderId = "ORDER-1003",
                Status = PaymentTransactionStatus.Captured,
                PaidAt = now,
                SubscriptionId = 3,
                DiscountId = null
            },
            new PaymentTransaction
            {
                PayerName = "Emma Morrison",
                Amount = 0m,
                Currency = "CAD",
                Provider = "PayPal",
                ProviderOrderId = "FREE-ORDER-1004",
                Status = PaymentTransactionStatus.Captured,
                PaidAt = now,
                SubscriptionId = 4,
                DiscountId = null
            }
        };

        _db.PaymentTransactions.AddRange(transactions);
        await _db.SaveChangesAsync();
    }
}
