using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TeamYellow.Models;

namespace TeamYellow.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Counsellor> Counsellors { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Counsellor>()
                .HasOne(c => c.User)
                .WithOne(u => u.Counsellor)
                .HasForeignKey<Counsellor>(c => c.UserId)
                //What is our bussiness logic?
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Counsellor>()
                .HasIndex(c => c.PractitionerLicenceId)
                .IsUnique();

            builder.Entity<Counsellor>()
                .HasIndex(c => c.UserId)
                .IsUnique();
        }
    }
}
