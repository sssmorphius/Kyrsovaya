using System.ComponentModel.DataAnnotations;

namespace AuthService.Dto
{
    public class CreateTeamRequest
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = default!;

        [Required]
        [StringLength(20)]
        public string Tag { get; set; } = default!;

        [Required]
        [RegularExpression("^(Dota2|CS2)$")]
        public string Game { get; set; } = default!;

        public string? LogoUrl { get; set; }
    }

    public class AddPlayerToTeamRequest
    {
        [Required]
        public Guid UserId { get; set; }
    }

    public class TeamResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public string Tag { get; set; } = default!;
        public string Game { get; set; } = default!;
        public Guid CaptainId { get; set; }
        public string CaptainName { get; set; } = default!;
        public string? LogoUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<TeamMemberResponse> Members { get; set; } = new();
    }

    public class TeamMemberResponse
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string Role { get; set; } = default!;
        public DateTime JoinedAt { get; set; }
    }
}
