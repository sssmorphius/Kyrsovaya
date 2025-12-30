using AuthService.Models;
using System.ComponentModel.DataAnnotations;

namespace AuthService.Model
{
    public class TeamMember
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TeamId { get; set; }
        public Team Team { get; set; } = default!;

        public Guid UserId { get; set; }
        public ApplicationUser User { get; set; } = default!;

        [StringLength(20)]
        public string Role { get; set; } = "Member"; // "Captain" или "Member"

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}
