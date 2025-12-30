using PlayerService.Models;
using System.ComponentModel.DataAnnotations;

namespace PlayerService.Dto
{
    public class CreateApplicationRequest
    {
        [Required]
        public Guid TournamentId { get; set; }

        [Required]
        public Guid TeamId { get; set; }

        [StringLength(500)]
        public string? AdditionalInfo { get; set; }
    }

    public class ApplicationResponse
    {
        public Guid Id { get; set; }
        public Guid TournamentId { get; set; }
        public string TournamentTitle { get; set; } = string.Empty; // Добавили название турнира
        public Guid TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string Game { get; set; } = string.Empty;
        public ApplicationStatus Status { get; set; }
        public DateTime AppliedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? RejectionReason { get; set; }
        public Guid? ReviewedById { get; set; }
        public Guid AppliedByCaptainId { get; set; }
    }

    public class ReviewApplicationRequest
    {
        [Required]
        public bool IsApproved { get; set; }

        [StringLength(500)]
        public string? Reason { get; set; }
    }

    public class TournamentApplicationsResponse
    {
        public Guid TournamentId { get; set; }
        public string TournamentTitle { get; set; } = string.Empty;
        public string TournamentStatus { get; set; } = string.Empty;
        public List<ApplicationResponse> Applications { get; set; } = new();
        public int TotalPending { get; set; }
        public int TotalApproved { get; set; }
        public int TotalRejected { get; set; }
        public int TotalCancelled { get; set; }
        public int TotalWithdrawn { get; set; }
    }

    public class TournamentInfoResponse
    {
        public Guid TournamentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Game { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int MaxTeams { get; set; }
        public int CurrentTeams { get; set; }
        public int ApprovedApplicationsCount { get; set; }
        public List<ApplicationResponse> ApprovedApplications { get; set; } = new();
        public List<ApplicationResponse> PendingApplications { get; set; } = new();
    }
}
