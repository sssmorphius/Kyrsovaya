using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PlayerService.Models;
using Shared.Contracts.Events;

namespace PlayerService.Consumers
{
    public class TeamMemberChangedConsumer : IConsumer<TeamMemberChangedEvent>
    {
        private readonly ILogger<TeamMemberChangedConsumer> _logger;
        private readonly ParticipantDbContext _context;

        public TeamMemberChangedConsumer(
            ILogger<TeamMemberChangedConsumer> logger,
            ParticipantDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task Consume(ConsumeContext<TeamMemberChangedEvent> context)
        {
            var message = context.Message;
            _logger.LogInformation("Team {TeamId} member count: {MemberCount}",
                message.TeamId, message.MemberCount);

            if (message.MemberCount < 5)
            {
                var pendingApplications = await _context.Applications
                    .Where(a => a.TeamId == message.TeamId && a.Status == ApplicationStatus.Pending)
                    .ToListAsync();

                foreach (var app in pendingApplications)
                {
                    app.Status = ApplicationStatus.Rejected;
                    app.RejectionReason = "Team has less than 5 players";
                    app.ReviewedAt = DateTime.UtcNow;
                }

                if (pendingApplications.Any())
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Rejected {Count} applications for team {TeamId}",
                        pendingApplications.Count, message.TeamId);
                }
            }
        }
    }
}