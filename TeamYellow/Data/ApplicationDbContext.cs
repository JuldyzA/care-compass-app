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

        public DbSet<Counsellor> Counsellors { get; set; }
        
        public DbSet<Client> Clients { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Counsellor>(entity =>
            {
                entity
                    .HasOne(c => c.User)
                    .WithOne()
                    .HasForeignKey<Counsellor>(c => c.UserId)
                    .OnDelete(DeleteBehavior.NoAction);

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
                    .HasForeignKey(cl => cl.CounsellorId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity
                    .HasIndex(cl => cl.Email)
                    .IsUnique();

                entity.Property(cl => cl.CreatedAt)
                      .HasDefaultValueSql("CURRENT_TIMESTAMP");
            });
        }
    }
}
