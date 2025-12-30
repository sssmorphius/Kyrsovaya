using MassTransit;
using PlayerService.Models;
using Shared.Contracts.Events;

namespace PlayerService.Consumers
{
    public class TournamentUpdatedConsumer : IConsumer<TournamentUpdatedEvent>
    {
        private readonly ILogger<TournamentUpdatedConsumer> _logger;
        private readonly ParticipantDbContext _context;

        public TournamentUpdatedConsumer(ILogger<TournamentUpdatedConsumer> logger, ParticipantDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task Consume(ConsumeContext<TournamentUpdatedEvent> context)
        {
            var message = context.Message;
            _logger.LogInformation("Tournament updated: {TournamentId}, Title: {Title}, Status: {Status}",
                message.TournamentId, message.Title, message.Status);

        }
    }
}
