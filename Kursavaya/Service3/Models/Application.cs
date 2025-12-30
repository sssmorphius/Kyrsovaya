using System.ComponentModel.DataAnnotations;

namespace PlayerService.Models
{
    public class TournamentApplication
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid TournamentId { get; set; }

        [Required]
        public Guid TeamId { get; set; }

        [Required]
        [StringLength(100)]
        public string TeamName { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string Game { get; set; } = string.Empty; // "Dota2" or "CS2"

        [Required]
        [StringLength(20)]
        public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;

        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedAt { get; set; }

        [StringLength(500)]
        public string? RejectionReason { get; set; }

        public Guid? ReviewedById { get; set; }


        public Guid AppliedByCaptainId { get; set; }
    }

    public enum ApplicationStatus
    {
        Pending,
        Approved,
        Rejected,
        Cancelled,
        Withdrawn
    }
}
