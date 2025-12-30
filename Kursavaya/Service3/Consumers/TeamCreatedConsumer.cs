using MassTransit;
using Microsoft.EntityFrameworkCore; 
using PlayerService.Models;
using Shared.Contracts.Events; 

namespace PlayerService.Consumers
{
    public class TeamCreatedConsumer : IConsumer<TeamCreatedEvent>
    {
        private readonly ILogger<TeamCreatedConsumer> _logger;
        private readonly ParticipantDbContext _context;

        public TeamCreatedConsumer(
            ILogger<TeamCreatedConsumer> logger,
            ParticipantDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task Consume(ConsumeContext<TeamCreatedEvent> context)
        {
            var message = context.Message;
            _logger.LogInformation("Team created: {TeamName} ({TeamId})",
                message.TeamName, message.TeamId);

        }
    }
}