namespace TeamYellow.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<UserProfile> UserProfiles { get; set; } = null!;
        public DbSet<UserLog> UserLogs { get; set; } = null!;
        public DbSet<Counsellor> Counsellors { get; set; } = null!;
        public DbSet<Client> Clients { get; set; } = null!;
        public DbSet<Plan> Plans { get; set; } = null!;
        public DbSet<PlanFeature> PlanFeatures { get; set; } = null!;
        public DbSet<Subscription> Subscriptions { get; set; } = null!;
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; } = null!;
        public DbSet<Discount> Discounts { get; set; } = null!;
        public DbSet<PlanDiscount> PlanDiscounts { get; set; } = null!;

        /// <summary>
        /// Configures entity models and seeds data.
        /// </summary>
        /// <param name="modelBuilder">The model builder instance.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // UserProfile: one-to-one with User, unique fkUserId
            modelBuilder.Entity<UserProfile>(entity =>
            {
                entity.HasIndex(e => e.UserId).IsUnique();
                entity.HasOne(e => e.User)
                    .WithOne()
                    .HasForeignKey<UserProfile>(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // UserLog: many-to-one with User
            modelBuilder.Entity<UserLog>(entity =>
            {
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Subscription: many-to-one with Plan and Counsellor
            modelBuilder.Entity<Subscription>(entity =>
            {
                entity.Property(e => e.Status)
                    .HasConversion<string>()
                    .HasMaxLength(20);

                entity.HasOne(e => e.Plan)
                    .WithMany(p => p.Subscriptions)
                    .HasForeignKey(e => e.PlanId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Counsellor)
                    .WithMany(c => c.Subscriptions)
                    .HasForeignKey(e => e.CounsellorId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // PlanDiscount: composite primary key, many-to-many junction table
            modelBuilder.Entity<PlanDiscount>(entity =>
            {
                entity.HasKey(e => new { e.PlanId, e.DiscountId });

                entity.HasOne(e => e.Plan)
                    .WithMany(p => p.PlanDiscounts)
                    .HasForeignKey(e => e.PlanId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Discount)
                    .WithMany(d => d.PlanDiscounts)
                    .HasForeignKey(e => e.DiscountId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // PaymentTransaction: one-to-one with Subscription, many-to-one with Discount (nullable)
            modelBuilder.Entity<PaymentTransaction>(entity =>
            {
                entity.HasIndex(e => e.SubscriptionId).IsUnique();
                entity.HasOne(e => e.Subscription)
                    .WithOne(s => s.PaymentTransaction)
                    .HasForeignKey<PaymentTransaction>(e => e.SubscriptionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Discount)
                    .WithMany(d => d.PaymentTransactions)
                    .HasForeignKey(e => e.DiscountId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Discount: unique discountCode
            modelBuilder.Entity<Discount>(entity =>
            {
                entity.HasIndex(e => e.DiscountCode).IsUnique();
            });

            // PlanFeature: many-to-one with Plan
            modelBuilder.Entity<PlanFeature>(entity =>
            {
                entity.HasOne(e => e.Plan)
                    .WithMany(p => p.PlanFeatures)
                    .HasForeignKey(e => e.PlanId)
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

                // value decimal(10,2) NOT NULL
                entity.Property(d => d.Value)
                      .HasConversion<double>()
                      .IsRequired();

                // createdAt datetime NOT NULL
                entity.Property(d => d.CreatedAt);
            });


            // Counsellor: one-to-one with User, unique fkUserId and practitionerLicenceId
            modelBuilder.Entity<Counsellor>(entity =>
            {
                // entity.HasIndex(e => e.UserId).IsUnique();

                entity
                    .HasOne(c => c.User)
                    .WithOne()
                    .HasForeignKey<Counsellor>(c => c.UserId)
                    .OnDelete(DeleteBehavior.NoAction)
                    //  .OnDelete(DeleteBehavior.Cascade);
                    // .HasIndex(e => e.PractitionerLicenceId).IsUnique()
                    ;

                entity
                    .HasIndex(c => c.PractitionerLicenceId)
                    .IsUnique();

                entity
                    .HasIndex(c => c.UserId)
                    .IsUnique();

                entity.Property(c => c.CreatedAt)
                      .HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            modelBuilder.Entity<Client>(entity =>
            {
                entity
                    .HasOne(cl => cl.Counsellor)
                    .WithMany()
                    // .WithMany(c => c.Clients) 
                    .HasForeignKey(cl => cl.CounsellorId)
                    .OnDelete(DeleteBehavior.Restrict);
                // .OnDelete(DeleteBehavior.Cascade);

                entity
                    .HasIndex(cl => cl.Email)
                    .IsUnique();

                entity.Property(cl => cl.CreatedAt)
                      .HasDefaultValueSql("CURRENT_TIMESTAMP");
            });
        }
    }
}
