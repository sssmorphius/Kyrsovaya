using AuthService.Models;
using System.ComponentModel.DataAnnotations;

namespace AuthService.Model
{
    public class Team
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = default!;

        [Required]
        [StringLength(20)]
        public string Tag { get; set; } = default!;

        [Required]
        [StringLength(20)]
        [RegularExpression("^(Dota2|CS2)$")]
        public string Game { get; set; } = default!;

        public Guid CaptainId { get; set; }
        public ApplicationUser Captain { get; set; } = default!;

        public string? LogoUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<TeamMember> Members { get; set; } = new List<TeamMember>();
    }
}
