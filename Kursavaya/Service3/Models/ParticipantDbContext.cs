using Microsoft.EntityFrameworkCore;

namespace PlayerService.Models
{
    public class ParticipantDbContext : DbContext
    {
        public DbSet<TournamentApplication> Applications { get; set; }

        public ParticipantDbContext(DbContextOptions<ParticipantDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasDefaultSchema("participant");

            // TournamentApplication configuration
            modelBuilder.Entity<TournamentApplication>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.TournamentId);
                entity.HasIndex(e => e.TeamId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.AppliedAt);

                entity.Property(e => e.Status)
                    .HasConversion<string>()
                    .HasMaxLength(20);

                entity.Property(e => e.Game)
                    .HasMaxLength(10);

                entity.Property(e => e.TeamName)
                    .HasMaxLength(100);

                entity.Property(e => e.RejectionReason)
                    .HasMaxLength(500);

                // Уникальная заявка команды на турнир
                entity.HasIndex(e => new { e.TournamentId, e.TeamId })
                    .IsUnique();
            });
        }
    }
}
