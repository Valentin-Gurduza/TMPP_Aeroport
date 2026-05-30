using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TMPP_Aeroport.Models;

namespace TMPP_Aeroport.Data
{
    // Inherits from IdentityDbContext to include all Identity tables (AspNetUsers, AspNetRoles, etc.)
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Airport Domain Entities
        public DbSet<Aircraft> Aircrafts { get; set; }
        public DbSet<Flight> Flights { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<BoardingPass> BoardingPasses { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<AirportMessage> AirportMessages { get; set; }
        public DbSet<GateAssignment> GateAssignments { get; set; }
        public DbSet<BaggageItem> BaggageItems { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Customize the ASP.NET Identity model and override the defaults if needed.

            builder.Entity<Ticket>()
                .HasIndex(t => new { t.FlightId, t.SeatNumber })
                .IsUnique()
                .HasFilter("[TicketState] <> 'Cancelled'");
        }
    }
}
