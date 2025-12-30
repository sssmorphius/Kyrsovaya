using Microsoft.EntityFrameworkCore;

namespace TournamentService.Models
{
    public class TournamentDbContext : DbContext
    {
        public DbSet<Tournament> Tournaments { get; set; }

        public TournamentDbContext(DbContextOptions<TournamentDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.HasDefaultSchema("tournament");
            builder.Entity<Tournament>(entity =>
            {
                entity.HasIndex(t => t.Status);
                entity.HasIndex(t => t.Game);
                entity.HasIndex(t => t.OrganizerId);
                entity.HasIndex(t => t.CreatedAt);
            });
        }
    }
}
