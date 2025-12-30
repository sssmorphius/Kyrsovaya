using System.ComponentModel.DataAnnotations;

namespace TournamentService.Dto
{
    public class CreateTournamentRequest
    {
        [Required]
        public string Title { get; set; } = default!;

        public string? Description { get; set; }

        [Required]
        [RegularExpression("^(Dota2|CS2)$")]
        public string Game { get; set; } = default!;

        [Required]
        public string Format { get; set; } = "SingleElimination";

        [Range(2, 128)]
        public int MaxTeams { get; set; } = 16;

        [Range(0, 1000000)]
        public decimal PrizePool { get; set; } = 0;

        public DateTime? RegistrationStart { get; set; }
        public DateTime? RegistrationEnd { get; set; }
        public DateTime? TournamentStart { get; set; }

        public string? StreamUrl { get; set; }
    }

    public class UpdateTournamentRequest
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public int? MaxTeams { get; set; }
        public decimal? PrizePool { get; set; }
        public DateTime? RegistrationStart { get; set; }
        public DateTime? RegistrationEnd { get; set; }
        public DateTime? TournamentStart { get; set; }
        public string? StreamUrl { get; set; }
    }

    public class TournamentResponse
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = default!;
        public string? Description { get; set; }
        public string Game { get; set; } = default!;
        public string Format { get; set; } = default!;
        public int MaxTeams { get; set; }
        public int CurrentTeams { get; set; }
        public Guid OrganizerId { get; set; }
        public string? OrganizerName { get; set; }
        public decimal PrizePool { get; set; }
        public string Status { get; set; } = default!;
        public DateTime? RegistrationStart { get; set; }
        public DateTime? RegistrationEnd { get; set; }
        public DateTime? TournamentStart { get; set; }
        public string? StreamUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
