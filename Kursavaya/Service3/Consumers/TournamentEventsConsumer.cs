using MassTransit;
using Microsoft.EntityFrameworkCore;
using PlayerService.Models;
using Shared.Contracts.Events;

namespace PlayerService.Consumers
{
    public class TournamentStatusChangedConsumer : IConsumer<TournamentStatusChangedEvent>
    {
        private readonly ILogger<TournamentStatusChangedConsumer> _logger;
        private readonly ParticipantDbContext _context;

        public TournamentStatusChangedConsumer(ILogger<TournamentStatusChangedConsumer> logger, ParticipantDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task Consume(ConsumeContext<TournamentStatusChangedEvent> context)
        {
            var message = context.Message;
            _logger.LogInformation("Tournament {TournamentId} status: {OldStatus} -> {NewStatus}",
                message.TournamentId, message.OldStatus, message.NewStatus);

            if (message.NewStatus == "Cancelled")
            {
                var applications = await _context.Applications
                    .Where(a => a.TournamentId == message.TournamentId)
                    .ToListAsync();

                foreach (var app in applications)
                {
                    if (app.Status != ApplicationStatus.Rejected &&
                        app.Status != ApplicationStatus.Cancelled)
                    {
                        app.Status = ApplicationStatus.Cancelled;
                        app.RejectionReason = "Tournament was cancelled";
                        app.ReviewedAt = DateTime.UtcNow;
                    }
                }

                if (applications.Any())
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Cancelled {Count} applications for cancelled tournament {TournamentId}",
                        applications.Count, message.TournamentId);
                }
            }
        }
    }
}
