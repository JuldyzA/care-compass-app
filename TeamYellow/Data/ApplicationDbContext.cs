using Microsoft.AspNetCore.Identity;
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

        public DbSet<Discount> Discounts { get; set; }

        /// <summary>
        /// Configures entity models and seeds data.
        /// </summary>
        /// <param name="modelBuilder">The model builder instance.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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
                entity.Property(d => d.CreatedAt)
                      .HasDefaultValueSql("CURRENT_TIMESTAMP");
            });
        }
    }
}
