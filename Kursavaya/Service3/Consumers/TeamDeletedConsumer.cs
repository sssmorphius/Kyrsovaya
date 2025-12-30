using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PlayerService.Models;
using Shared.Contracts.Events;

namespace PlayerService.Consumers
{
    public class TeamDeletedConsumer : IConsumer<TeamDeletedEvent>
    {
        private readonly ILogger<TeamDeletedConsumer> _logger;
        private readonly ParticipantDbContext _context;

        public TeamDeletedConsumer(
            ILogger<TeamDeletedConsumer> logger,
            ParticipantDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task Consume(ConsumeContext<TeamDeletedEvent> context)
        {
            var message = context.Message;
            _logger.LogInformation("Team {TeamId} deleted", message.TeamId);

            var teamApplications = await _context.Applications
                .Where(a => a.TeamId == message.TeamId)
                .ToListAsync();

            foreach (var app in teamApplications)
            {
                if (app.Status == ApplicationStatus.Pending ||
                    app.Status == ApplicationStatus.Approved)
                {
                    app.Status = ApplicationStatus.Cancelled;
                    app.RejectionReason = "Team was deleted";
                    app.ReviewedAt = DateTime.UtcNow;
                }
            }

            if (teamApplications.Any())
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Cancelled {Count} applications for deleted team {TeamId}",
                    teamApplications.Count, message.TeamId);
            }
        }
    }
}