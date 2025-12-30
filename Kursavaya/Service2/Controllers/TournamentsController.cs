using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts.Events;
using System.Security.Claims;
using TournamentService.Dto;
using TournamentService.Models;

namespace TournamentService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TournamentsController : ControllerBase
    {
        private readonly TournamentDbContext _context;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<TournamentsController> _logger;

        public TournamentsController(
            TournamentDbContext context,
            IPublishEndpoint publishEndpoint,
            ILogger<TournamentsController> logger)
        {
            _context = context;
            _publishEndpoint = publishEndpoint;
            _logger = logger;
        }

        // 1. Создание турнира (только организатор)
        [HttpPost]
        [Authorize(Roles = "Organizer,Admin")]
        public async Task<IActionResult> CreateTournament([FromBody] CreateTournamentRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var organizerName = User.FindFirstValue(ClaimTypes.Name) ?? "Unknown";

            var tournament = new Tournament
            {
                Title = request.Title,
                Description = request.Description,
                Game = request.Game,
                Format = request.Format,
                MaxTeams = request.MaxTeams,
                OrganizerId = Guid.Parse(userId),
                OrganizerName = organizerName,
                PrizePool = request.PrizePool,
                Status = "Draft",
                RegistrationStart = request.RegistrationStart,
                RegistrationEnd = request.RegistrationEnd,
                TournamentStart = request.TournamentStart,
                StreamUrl = request.StreamUrl,
                CreatedAt = DateTime.UtcNow
            };

            _context.Tournaments.Add(tournament);
            await _context.SaveChangesAsync();

            return Ok(new TournamentResponse
            {
                Id = tournament.Id,
                Title = tournament.Title,
                Description = tournament.Description,
                Game = tournament.Game,
                Format = tournament.Format,
                MaxTeams = tournament.MaxTeams,
                CurrentTeams = tournament.CurrentTeams,
                OrganizerId = tournament.OrganizerId,
                OrganizerName = tournament.OrganizerName,
                PrizePool = tournament.PrizePool,
                Status = tournament.Status,
                RegistrationStart = tournament.RegistrationStart,
                RegistrationEnd = tournament.RegistrationEnd,
                TournamentStart = tournament.TournamentStart,
                StreamUrl = tournament.StreamUrl,
                CreatedAt = tournament.CreatedAt
            });
        }

        // 2. Получение списка турниров (публичный, с фильтрами)
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetTournaments(
            [FromQuery] string? game,
            [FromQuery] string? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = _context.Tournaments.AsQueryable();

            if (!string.IsNullOrEmpty(game))
                query = query.Where(t => t.Game == game);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(t => t.Status == status);

            var totalCount = await query.CountAsync();
            var tournaments = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new TournamentResponse
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    Game = t.Game,
                    Format = t.Format,
                    MaxTeams = t.MaxTeams,
                    CurrentTeams = t.CurrentTeams,
                    OrganizerId = t.OrganizerId,
                    OrganizerName = t.OrganizerName,
                    PrizePool = t.PrizePool,
                    Status = t.Status,
                    RegistrationStart = t.RegistrationStart,
                    RegistrationEnd = t.RegistrationEnd,
                    TournamentStart = t.TournamentStart,
                    StreamUrl = t.StreamUrl,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                Tournaments = tournaments
            });
        }

        // 3. Получение информации о конкретном турнире (публичный)
        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTournament(Guid id)
        {
            var tournament = await _context.Tournaments
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tournament == null)
                return NotFound("Tournament not found");

            return Ok(new TournamentResponse
            {
                Id = tournament.Id,
                Title = tournament.Title,
                Description = tournament.Description,
                Game = tournament.Game,
                Format = tournament.Format,
                MaxTeams = tournament.MaxTeams,
                CurrentTeams = tournament.CurrentTeams,
                OrganizerId = tournament.OrganizerId,
                OrganizerName = tournament.OrganizerName,
                PrizePool = tournament.PrizePool,
                Status = tournament.Status,
                RegistrationStart = tournament.RegistrationStart,
                RegistrationEnd = tournament.RegistrationEnd,
                TournamentStart = tournament.TournamentStart,
                StreamUrl = tournament.StreamUrl,
                CreatedAt = tournament.CreatedAt
            });
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Organizer,Admin")]
        public async Task<IActionResult> UpdateTournament(
    Guid id,
    [FromBody] UpdateTournamentRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var tournament = await _context.Tournaments
                .FirstOrDefaultAsync(t => t.Id == id && t.OrganizerId == Guid.Parse(userId));

            if (tournament == null)
                return NotFound("Tournament not found or you are not the organizer");

            // Можно редактировать только турниры в статусе Draft
            if (tournament.Status != "Draft")
                return BadRequest($"Cannot update tournament with status '{tournament.Status}'");

            // Сохраняем старые значения для проверки изменений
            var oldTitle = tournament.Title;
            var oldGame = tournament.Game;
            var oldMaxTeams = tournament.MaxTeams;

            if (request.Title != null) tournament.Title = request.Title;
            if (request.Description != null) tournament.Description = request.Description;
            if (request.MaxTeams.HasValue) tournament.MaxTeams = request.MaxTeams.Value;
            if (request.PrizePool.HasValue) tournament.PrizePool = request.PrizePool.Value;
            if (request.RegistrationStart.HasValue) tournament.RegistrationStart = request.RegistrationStart.Value;
            if (request.RegistrationEnd.HasValue) tournament.RegistrationEnd = request.RegistrationEnd.Value;
            if (request.TournamentStart.HasValue) tournament.TournamentStart = request.TournamentStart.Value;
            if (request.StreamUrl != null) tournament.StreamUrl = request.StreamUrl;

            tournament.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Публикуем событие обновления турнира (если были изменения важных полей)
            if (oldTitle != tournament.Title || oldGame != tournament.Game || oldMaxTeams != tournament.MaxTeams)
            {
                await _publishEndpoint.Publish<TournamentUpdatedEvent>(new
                {
                    TournamentId = tournament.Id,
                    Title = tournament.Title,
                    Game = tournament.Game,
                    Status = tournament.Status,
                    MaxTeams = tournament.MaxTeams,
                    UpdatedAt = tournament.UpdatedAt
                });

                _logger.LogInformation("Tournament {TournamentId} updated", tournament.Id);
            }

            return Ok("Tournament updated successfully");
        }
        // 5. Изменение статуса турнира (только организатор)
        [HttpPost("{id:guid}/status")]
        [Authorize(Roles = "Organizer,Admin")]
        public async Task<IActionResult> UpdateTournamentStatus(
    Guid id,
    [FromBody] string newStatus)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var tournament = await _context.Tournaments
                .FirstOrDefaultAsync(t => t.Id == id && t.OrganizerId == Guid.Parse(userId));

            if (tournament == null)
                return NotFound("Tournament not found or you are not the organizer");

            var validTransitions = new Dictionary<string, List<string>>
            {
                ["Draft"] = new List<string> { "Registration", "Cancelled" },
                ["Registration"] = new List<string> { "Live", "Cancelled" },
                ["Live"] = new List<string> { "Finished", "Cancelled" },
                ["Finished"] = new List<string>(),
                ["Cancelled"] = new List<string>()
            };

            if (!validTransitions.ContainsKey(tournament.Status) ||
                !validTransitions[tournament.Status].Contains(newStatus))
            {
                return BadRequest($"Cannot change status from '{tournament.Status}' to '{newStatus}'");
            }

            var oldStatus = tournament.Status; // Сохраняем старый статус
            tournament.Status = newStatus;
            tournament.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Публикуем событие изменения статуса турнира
            await _publishEndpoint.Publish<TournamentStatusChangedEvent>(new
            {
                TournamentId = tournament.Id,
                OldStatus = oldStatus,
                NewStatus = newStatus,
                ChangedAt = tournament.UpdatedAt
            });

            _logger.LogInformation("Tournament {TournamentId} status changed: {OldStatus} -> {NewStatus}",
                tournament.Id, oldStatus, newStatus);

            return Ok($"Tournament status changed to {newStatus}");
        }

        // 6. Получение моих турниров (для организатора)
        [HttpGet("my")]
        [Authorize(Roles = "Organizer,Admin")]
        public async Task<IActionResult> GetMyTournaments(
            [FromQuery] string? status,
            [FromQuery] string? game)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var query = _context.Tournaments
                .Where(t => t.OrganizerId == Guid.Parse(userId))
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(t => t.Status == status);

            if (!string.IsNullOrEmpty(game))
                query = query.Where(t => t.Game == game);

            var tournaments = await query
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new TournamentResponse
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    Game = t.Game,
                    Format = t.Format,
                    MaxTeams = t.MaxTeams,
                    CurrentTeams = t.CurrentTeams,
                    OrganizerId = t.OrganizerId,
                    OrganizerName = t.OrganizerName,
                    PrizePool = t.PrizePool,
                    Status = t.Status,
                    RegistrationStart = t.RegistrationStart,
                    RegistrationEnd = t.RegistrationEnd,
                    TournamentStart = t.TournamentStart,
                    StreamUrl = t.StreamUrl,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();

            return Ok(tournaments);
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Organizer,Admin")]
        public async Task<IActionResult> DeleteTournament(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var tournament = await _context.Tournaments
                .FirstOrDefaultAsync(t => t.Id == id && t.OrganizerId == Guid.Parse(userId));

            if (tournament == null)
                return NotFound("Tournament not found or you are not the organizer");

            if (tournament.Status != "Draft")
                return BadRequest($"Cannot delete tournament with status '{tournament.Status}'");

            _context.Tournaments.Remove(tournament);
            await _context.SaveChangesAsync();

            return Ok("Tournament deleted successfully");
        }
    }
}
