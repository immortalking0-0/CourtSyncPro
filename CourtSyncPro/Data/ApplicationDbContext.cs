using CourtSyncPro.Models.Entities;
using Microsoft.EntityFrameworkCore;
namespace CourtSyncPro.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<CourtOwner> CourtOwners { get; set; }
        public DbSet<Court> Courts { get; set; }
        public DbSet<TimeSlot> TimeSlots { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Membership> Memberships { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Admin> Admins { get; set; }

        // Add these two DbSets with the others
        public DbSet<Tournament> Tournaments { get; set; }
        public DbSet<TournamentRegistration> TournamentRegistrations { get; set; }

        // ↓ PASTE HERE — replace the old OnModelCreating entirely
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email).IsUnique();

            modelBuilder.Entity<Court>()
                .HasOne(c => c.CourtOwner)
                .WithMany(o => o.Courts)
                .HasForeignKey(c => c.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.User)
                .WithMany(u => u.Bookings)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Court)
                .WithMany(c => c.Bookings)
                .HasForeignKey(b => b.CourtId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.TimeSlot)
                .WithMany(t => t.Bookings)
                .HasForeignKey(b => b.SlotId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Booking)
                .WithOne(b => b.Payment)
                .HasForeignKey<Payment>(p => p.BookingId)
                .OnDelete(DeleteBehavior.Cascade);


            // Tournament → Court (many-to-one)
            modelBuilder.Entity<Tournament>()
                .HasOne(t => t.Court)
                .WithMany()
                .HasForeignKey(t => t.CourtId)
                .OnDelete(DeleteBehavior.Restrict);

            // Tournament → Registrations (one-to-many)
            modelBuilder.Entity<TournamentRegistration>()
                .HasOne(r => r.Tournament)
                .WithMany(t => t.Registrations)
                .HasForeignKey(r => r.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);

            // User → TournamentRegistrations (one-to-many)
            modelBuilder.Entity<TournamentRegistration>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);


            modelBuilder.Entity<Admin>().HasData(new Admin
                {
                    AdminId = 1,
                    Name = "Super Admin",
                    Email = "admin@courtsync.com",
                    // BCrypt hash of "Admin@1234" — change this before production!
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@1234")
                });
        }
    }
}


