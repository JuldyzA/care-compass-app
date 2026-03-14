using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TeamYellow.Models;

namespace TeamYellow.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<UserLog> UserLogs { get; set; }
        public DbSet<Counsellor> Counsellors { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<Plan> Plans { get; set; }
        public DbSet<PlanFeature> PlanFeatures { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
        public DbSet<Discount> Discounts { get; set; }
        public DbSet<PlanDiscount> PlanDiscounts { get; set; }

        /// <summary>
        /// Configures entity models and seeds data.
        /// </summary>
        /// <param name="modelBuilder">The model builder instance.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Business Rules
            // UserProfile: one-to-one with User, unique fkUserId
            modelBuilder.Entity<UserProfile>(entity =>
            {
                entity.HasIndex(u => u.UserId).IsUnique();
                entity.HasOne(u => u.User)
                    .WithOne()
                    .HasForeignKey<UserProfile>(u => u.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(u => u.CreatedAt)
                      .HasDefaultValueSql("CURRENT_TIMESTAMP")
                      .ValueGeneratedOnAdd();
            });

            // UserLog: many-to-one with User
            modelBuilder.Entity<UserLog>(entity =>
            {
                entity.HasOne(ul => ul.User)
                    .WithMany()
                    .HasForeignKey(ul => ul.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Subscription: many-to-one with Plan and Counsellor
            modelBuilder.Entity<Subscription>(entity =>
            {
                entity.Property(s => s.Status)
                    .HasConversion<string>()
                    .HasMaxLength(20);

                entity.HasOne(s => s.Plan)
                    .WithMany(p => p.Subscriptions)
                    .HasForeignKey(s => s.PlanId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(s => s.Counsellor)
                    .WithMany(c => c.Subscriptions)
                    .HasForeignKey(e => e.CounsellorId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // PlanDiscount: composite primary key, many-to-many junction table
            modelBuilder.Entity<PlanDiscount>(entity =>
            {
                entity.HasKey(pd => new { pd.PlanId, pd.DiscountId });

                entity.HasOne(pd => pd.Plan)
                    .WithMany(p => p.PlanDiscounts)
                    .HasForeignKey(pd => pd.PlanId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(pd => pd.Discount)
                    .WithMany(d => d.PlanDiscounts)
                    .HasForeignKey(pd => pd.DiscountId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Plan>(entity =>
            {
                entity.Property(p => p.CreatedAt)
                      .HasDefaultValueSql("CURRENT_TIMESTAMP")
                      .ValueGeneratedOnAdd();
            });

            // PaymentTransaction: one-to-one with Subscription, many-to-one with Discount (nullable)
            modelBuilder.Entity<PaymentTransaction>(entity =>
            {
                entity.HasIndex(pt => pt.SubscriptionId).IsUnique();
                entity.HasOne(pt => pt.Subscription)
                      .WithOne(s => s.PaymentTransaction)
                      .HasForeignKey<PaymentTransaction>(pt => pt.SubscriptionId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(pt => pt.Discount)
                    .WithMany(d => d.PaymentTransactions)
                    .HasForeignKey(pt => pt.DiscountId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.Property(pt => pt.PaidAt)
                      .HasDefaultValueSql("CURRENT_TIMESTAMP")
                      .ValueGeneratedOnAdd();

                entity.Property(pt => pt.Status)
                      .HasConversion<string>()
                      .HasMaxLength(20);
            });

            // PlanFeature: many-to-one with Plan
            modelBuilder.Entity<PlanFeature>(entity =>
            {
                entity.HasOne(pf => pf.Plan)
                    .WithMany(p => p.PlanFeatures)
                    .HasForeignKey(pf => pf.PlanId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<Discount>(entity =>
            {
                // Unique: discountCode
                entity.HasIndex(d => d.DiscountCode).IsUnique();

                // discountType varchar(10) NOT NULL, default is "%"
                entity.Property(d => d.DiscountType)
                      .HasConversion(
                        v => v == DiscountType.Percent ? "%" : "$",
                        v => v == "%" ? DiscountType.Percent : DiscountType.Amount)
                      .HasDefaultValue(DiscountType.Percent);

                // createdAt datetime NOT NULL
                entity.Property(d => d.CreatedAt)
                      .HasDefaultValueSql("CURRENT_TIMESTAMP")
                      .ValueGeneratedOnAdd();
            });


            // Counsellor: one-to-one with User, unique fkUserId and practitionerLicenceId
            modelBuilder.Entity<Counsellor>(entity =>
            {
                entity
                    .HasOne(c => c.User)
                    .WithOne()
                    .HasForeignKey<Counsellor>(c => c.UserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity
                    .HasIndex(c => c.PractitionerLicenceId)
                    .IsUnique();

                entity
                    .HasIndex(c => c.UserId)
                    .IsUnique();

                entity.Property(c => c.CreatedAt)
                      .HasDefaultValueSql("CURRENT_TIMESTAMP")
                      .ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<Client>(entity =>
            {
                entity
                    .HasOne(cl => cl.Counsellor)
                    .WithMany(c => c.Clients)
                    .HasForeignKey(cl => cl.CounsellorId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity
                    .HasIndex(cl => cl.Email)
                    .IsUnique();

                entity.Property(cl => cl.CreatedAt)
                      .HasDefaultValueSql("CURRENT_TIMESTAMP")
                      .ValueGeneratedOnAdd();

                entity.Property(cl => cl.Status)
                      .HasConversion<string>()
                      .HasMaxLength(20);
            });

            // Seed Plans
            modelBuilder.Entity<Plan>().HasData(
                new Plan
                {
                    PlanId = 1,
                    PlanName = "Free",
                    PlanDescription = "Free basic access",
                    Price = 0.00m,
                    BillingType = "Free",
                    IsActive = true,
                    CreatedAt = new DateTime(2025, 01, 01)
                },
                new Plan
                {
                    PlanId = 2,
                    PlanName = "Monthly",
                    PlanDescription = "Monthly subscription",
                    Price = 49.99m,
                    BillingType = "Monthly",
                    IsActive = true,
                    CreatedAt = new DateTime(2025, 01, 01)
                },
                new Plan
                {
                    PlanId = 3,
                    PlanName = "Yearly",
                    PlanDescription = "Yearly subscription",
                    Price = 499.99m,
                    BillingType = "Yearly",
                    IsActive = true,
                    CreatedAt = new DateTime(2025, 01, 01)
                }
            );

            // Seed Discounts
            modelBuilder.Entity<Discount>().HasData(
                new Discount
                {
                    DiscountId = 1,
                    DiscountCode = "WELCOME10",
                    DiscountType = DiscountType.Percent,
                    Value = 10.00m,
                    StartDateTime = new DateTime(2025, 01, 01),
                    EndDateTime = new DateTime(9999, 12, 31),
                    CreatedAt = new DateTime(2025, 01, 01)
                },
                new Discount
                {
                    DiscountId = 2,
                    DiscountCode = "YEARLY50",
                    DiscountType = DiscountType.Amount,
                    Value = 50.00m,
                    StartDateTime = new DateTime(2025, 01, 01),
                    EndDateTime = new DateTime(9999, 12, 31),
                    CreatedAt = new DateTime(2025, 01, 01)
                }
            );

            modelBuilder.Entity<PlanDiscount>().HasData(
                new PlanDiscount
                {
                    PlanId = 2,
                    DiscountId = 1
                },
                new PlanDiscount
                {
                    PlanId = 3,
                    DiscountId = 1
                },

                // YEARLY50 applies only to Yearly
                new PlanDiscount
                {
                    PlanId = 3,
                    DiscountId = 2
                }
            );

            modelBuilder.Entity<PlanFeature>().HasData(
                // Free Plan Features
                new PlanFeature
                {
                    PlanFeatureId = 1,
                    PlanId = 1,
                    FeatureName = "Basic Access",
                    FeatureDescription = "Access to limited resources and tools.",
                    SortOrder = 1
                },
                new PlanFeature
                {
                    PlanFeatureId = 2,
                    PlanId = 1,
                    FeatureName = "Community Support",
                    FeatureDescription = "Access to community forum support.",
                    SortOrder = 2
                },

                // Monthly Plan Features
                new PlanFeature
                {
                    PlanFeatureId = 3,
                    PlanId = 2,
                    FeatureName = "All Free Features",
                    FeatureDescription = "Includes all Free plan features.",
                    SortOrder = 1
                },
                new PlanFeature
                {
                    PlanFeatureId = 4,
                    PlanId = 2,
                    FeatureName = "Priority Support",
                    FeatureDescription = "Get help faster with priority support.",
                    SortOrder = 2
                },
                new PlanFeature
                {
                    PlanFeatureId = 5,
                    PlanId = 2,
                    FeatureName = "Advanced Analytics",
                    FeatureDescription = "Access to detailed reports and analytics.",
                    SortOrder = 3
                },

                // Yearly Plan Features
                new PlanFeature
                {
                    PlanFeatureId = 6,
                    PlanId = 3,
                    FeatureName = "All Monthly Features",
                    FeatureDescription = "Includes all Monthly plan features.",
                    SortOrder = 1
                },
                new PlanFeature
                {
                    PlanFeatureId = 7,
                    PlanId = 3,
                    FeatureName = "Dedicated Account Manager",
                    FeatureDescription = "Assigned a dedicated account manager for support.",
                    SortOrder = 2
                },
                new PlanFeature
                {
                    PlanFeatureId = 8,
                    PlanId = 3,
                    FeatureName = "Unlimited Storage",
                    FeatureDescription = "Store unlimited data and files.",
                    SortOrder = 3
                }
            );
        }
    }
}
