using AuthService.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace AuthService.Models
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        [StringLength(50)]
        public string? PlayerTag { get; set; } // Теперь опционально
        public string? AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<TeamMember> TeamMemberships { get; set; } = new List<TeamMember>();
        public virtual ICollection<Team> OwnedTeams { get; set; } = new List<Team>();
    }

    public class ApplicationRole : IdentityRole<Guid>
    {
        public ApplicationRole() : base() { }
        public ApplicationRole(string roleName) : base(roleName) { }
    }
    public class AuthDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
    {
        public DbSet<Team> Teams { get; set; }
        public DbSet<TeamMember> TeamMembers { get; set; }

        public AuthDbContext(DbContextOptions<AuthDbContext> options)
                : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<Team>(entity =>
            {
                entity.HasIndex(t => t.Name).IsUnique();
                entity.HasIndex(t => t.Tag).IsUnique();

                entity.HasOne(t => t.Captain)
                    .WithMany(u => u.OwnedTeams)
                    .HasForeignKey(t => t.CaptainId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<TeamMember>(entity =>
            {
                entity.HasIndex(tm => new { tm.TeamId, tm.UserId }).IsUnique();

                entity.HasOne(tm => tm.Team)
                    .WithMany(t => t.Members)
                    .HasForeignKey(tm => tm.TeamId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(tm => tm.User)
                    .WithMany(u => u.TeamMemberships)
                    .HasForeignKey(tm => tm.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            builder.Entity<ApplicationRole>().HasData(
               new ApplicationRole
               {
                   Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                   Name = RoleNames.Admin,
                   NormalizedName = RoleNames.Admin.ToUpper(),
                   ConcurrencyStamp = "11111111-1111-1111-1111-111111111111"
               },
               new ApplicationRole
               {
                   Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                   Name = RoleNames.Organizer,
                   NormalizedName = RoleNames.Organizer.ToUpper(),
                   ConcurrencyStamp = "22222222-2222-2222-2222-222222222222"
               },
               new ApplicationRole
               {
                   Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                   Name = RoleNames.Player,
                   NormalizedName = RoleNames.Player.ToUpper(),
                   ConcurrencyStamp = "33333333-3333-3333-3333-333333333333"
               }
           );

        }
    }
}
