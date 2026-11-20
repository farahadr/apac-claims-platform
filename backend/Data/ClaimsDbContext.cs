using ClaimsApi.Features.Claims;
using ClaimsApi.Features.Dashboard;
using Microsoft.EntityFrameworkCore;

namespace ClaimsApi.Data;

public class ClaimsDbContext(DbContextOptions<ClaimsDbContext> options) : DbContext(options)
{
    public DbSet<Claim> Claims => Set<Claim>();
    public DbSet<ClaimEvent> ClaimEvents => Set<ClaimEvent>();
    public DbSet<DashboardClaim> DashboardClaims => Set<DashboardClaim>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Save every enum as readable text ("UnderReview") instead of a number.
        configurationBuilder.Properties<Enum>().HaveConversion<string>().HaveMaxLength(32);

        // Every money amount: up to 18 digits, 2 of them after the decimal point.
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Claim>(claim =>
        {
            claim.Property(c => c.Market).HasMaxLength(50);
            claim.Property(c => c.Description).HasMaxLength(2000);
            claim.Property(c => c.Claimant).HasMaxLength(100);
            claim.Property(c => c.AssignedOfficer).HasMaxLength(100);

            claim.HasIndex(c => c.Status);

            // Safety net: the database rejects incident dates in the future.
            claim.ToTable(t => t.HasCheckConstraint(
                "CK_Claims_IncidentDate_NotInFuture",
                "\"IncidentDate\" <= CURRENT_DATE"));

            // A claim has many events. Events are an audit trail, so the
            // database refuses to delete a claim that still has history.
            claim.HasMany(c => c.Events)
                .WithOne(e => e.Claim)
                .HasForeignKey(e => e.ClaimId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ClaimEvent>(ev =>
        {
            ev.Property(e => e.ChangedBy).HasMaxLength(100);
            ev.Property(e => e.Note).HasMaxLength(1000);
        });

        // Filled by the Kafka consumer. No foreign key to Claims on purpose:
        // the dashboard only knows what it has read from Kafka.
        modelBuilder.Entity<DashboardClaim>(d =>
        {
            d.HasKey(x => x.ClaimId);
            d.Property(x => x.ClaimId).ValueGeneratedNever(); // the id comes from the message
            d.Property(x => x.AssignedOfficer).HasMaxLength(100);
        });
    }
}
