using System.ComponentModel.DataAnnotations;

namespace TournamentService.Models
{
    public class Tournament
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = default!;

        public string? Description { get; set; }

        [Required]
        [StringLength(20)]
        [RegularExpression("^(Dota2|CS2)$")]
        public string Game { get; set; } = default!;

        [Required]
        [StringLength(50)]
        public string Format { get; set; } = "SingleElimination";

        public int MaxTeams { get; set; } = 16;
        public int CurrentTeams { get; set; } = 0;

        [Required]
        public Guid OrganizerId { get; set; }

        [StringLength(100)]
        public string? OrganizerName { get; set; }

        public decimal PrizePool { get; set; } = 0;

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Draft";

        public DateTime? RegistrationStart { get; set; }
        public DateTime? RegistrationEnd { get; set; }
        public DateTime? TournamentStart { get; set; }

        public string? StreamUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
