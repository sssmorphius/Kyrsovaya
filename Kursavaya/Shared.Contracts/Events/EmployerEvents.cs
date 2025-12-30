namespace Shared.Contracts.Events
{
    // События от AuthService
    public record TeamCreatedEvent
    {
        public Guid TeamId { get; init; }
        public string TeamName { get; init; } = string.Empty;
        public string Game { get; init; } = string.Empty; // "Dota2" или "CS2"
        public Guid CaptainId { get; init; }
        public DateTime CreatedAt { get; init; }
    }
    
    public record TeamMemberChangedEvent
    {
        public Guid TeamId { get; init; }
        public int MemberCount { get; init; }
        public DateTime UpdatedAt { get; init; }
    }
    
    public record TeamDeletedEvent
    {
        public Guid TeamId { get; init; }
        public DateTime DeletedAt { get; init; }
    }
    
    // События от TournamentService
    public record TournamentStatusChangedEvent
    {
        public Guid TournamentId { get; init; }
        public string OldStatus { get; init; } = string.Empty;
        public string NewStatus { get; init; } = string.Empty;
        public DateTime ChangedAt { get; init; }
    }
    
    // События от PlayerService
    public record ApplicationSubmittedEvent
    {
        public Guid ApplicationId { get; init; }
        public Guid TournamentId { get; init; }
        public Guid TeamId { get; init; }
        public string TeamName { get; init; } = string.Empty;
        public DateTime SubmittedAt { get; init; }
    }
    
    public record ApplicationStatusChangedEvent
    {
        public Guid ApplicationId { get; init; }
        public Guid TournamentId { get; init; }
        public Guid TeamId { get; init; }
        public string OldStatus { get; init; } = string.Empty;
        public string NewStatus { get; init; } = string.Empty;
        public string? Reason { get; init; }
        public DateTime ChangedAt { get; init; }
    }
    public record TournamentUpdatedEvent
    {
        public Guid TournamentId { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Game { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public int MaxTeams { get; init; }
        public DateTime UpdatedAt { get; init; }
    }
}