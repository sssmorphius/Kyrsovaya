using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlayerService.Dto;
using PlayerService.Models;
using Shared.Contracts.Events;
using System.Security.Claims;

namespace PlayerService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ApplicationsController : ControllerBase
    {
        private readonly ParticipantDbContext _context;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<ApplicationsController> _logger;

        public ApplicationsController(
            ParticipantDbContext context,
            IPublishEndpoint publishEndpoint,
            ILogger<ApplicationsController> logger)
        {
            _context = context;
            _publishEndpoint = publishEndpoint;
            _logger = logger;
        }

        [HttpPost("apply")]
        [Authorize(Roles = "Player")]
        public async Task<IActionResult> ApplyForTournament([FromBody] CreateApplicationRequest request)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null || !Guid.TryParse(userId, out var captainId))
                    return Unauthorized("Invalid user");

                var isTeamValid = await ValidateTeamAndCaptain(request.TeamId, captainId);
                if (!isTeamValid)
                    return BadRequest("Team not found or you are not the captain");

                var isTournamentValid = await ValidateTournament(request.TournamentId);
                if (!isTournamentValid)
                    return BadRequest("Tournament not found or not accepting applications");

                var existingApplication = await _context.Applications
                    .FirstOrDefaultAsync(a => a.TournamentId == request.TournamentId &&
                                              a.TeamId == request.TeamId &&
                                              (a.Status == ApplicationStatus.Pending ||
                                               a.Status == ApplicationStatus.Approved));

                if (existingApplication != null)
                    return BadRequest("Team already applied for this tournament");

                var activeApplication = await _context.Applications
                    .FirstOrDefaultAsync(a => a.TeamId == request.TeamId &&
                                              a.Status == ApplicationStatus.Approved);

                if (activeApplication != null)
                {
                    var isTournamentActive = await IsTournamentActive(activeApplication.TournamentId);
                    if (isTournamentActive)
                        return BadRequest("Team is already participating in another active tournament");
                }

                var teamName = await GetTeamName(request.TeamId);
                var tournamentGame = await GetTournamentGame(request.TournamentId);

                var application = new TournamentApplication
                {
                    TournamentId = request.TournamentId,
                    TeamId = request.TeamId,
                    TeamName = teamName,
                    Game = tournamentGame,
                    Status = ApplicationStatus.Pending,
                    AppliedAt = DateTime.UtcNow,
                    AppliedByCaptainId = captainId
                };

                _context.Applications.Add(application);
                await _context.SaveChangesAsync();

                var tournamentTitle = await GetTournamentTitle(application.TournamentId);

                await _publishEndpoint.Publish(new ApplicationSubmittedEvent
                {
                    ApplicationId = application.Id,
                    TournamentId = application.TournamentId,
                    TeamId = application.TeamId,
                    TeamName = application.TeamName,
                    SubmittedAt = DateTime.UtcNow
                });

                _logger.LogInformation("Application {ApplicationId} submitted for tournament {TournamentId} by team {TeamId}",
                    application.Id, application.TournamentId, application.TeamId);

                return Ok(new ApplicationResponse
                {
                    Id = application.Id,
                    TournamentId = application.TournamentId,
                    TournamentTitle = tournamentTitle,
                    TeamId = application.TeamId,
                    TeamName = application.TeamName,
                    Game = application.Game,
                    Status = application.Status,
                    AppliedAt = application.AppliedAt,
                    AppliedByCaptainId = application.AppliedByCaptainId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting application");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("my")]
        [Authorize(Roles = "Player")]
        public async Task<IActionResult> GetMyApplications()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null || !Guid.TryParse(userId, out var captainId))
                return Unauthorized();

            var applications = await _context.Applications
                .Where(a => a.AppliedByCaptainId == captainId)
                .OrderByDescending(a => a.AppliedAt)
                .ToListAsync();

            var response = new List<ApplicationResponse>();
            foreach (var application in applications)
            {
                var tournamentTitle = await GetTournamentTitle(application.TournamentId);

                response.Add(new ApplicationResponse
                {
                    Id = application.Id,
                    TournamentId = application.TournamentId,
                    TournamentTitle = tournamentTitle,
                    TeamId = application.TeamId,
                    TeamName = application.TeamName,
                    Game = application.Game,
                    Status = application.Status,
                    AppliedAt = application.AppliedAt,
                    ReviewedAt = application.ReviewedAt,
                    RejectionReason = application.RejectionReason,
                    ReviewedById = application.ReviewedById,
                    AppliedByCaptainId = application.AppliedByCaptainId
                });
            }

            return Ok(response);
        }

        [HttpGet("team/{teamId:guid}")]
        [Authorize(Roles = "Player")]
        public async Task<IActionResult> GetTeamApplications(Guid teamId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            var applications = await _context.Applications
                .Where(a => a.TeamId == teamId)
                .OrderByDescending(a => a.AppliedAt)
                .ToListAsync();

            var response = new List<ApplicationResponse>();
            foreach (var application in applications)
            {
                var tournamentTitle = await GetTournamentTitle(application.TournamentId);

                response.Add(new ApplicationResponse
                {
                    Id = application.Id,
                    TournamentId = application.TournamentId,
                    TournamentTitle = tournamentTitle,
                    TeamId = application.TeamId,
                    TeamName = application.TeamName,
                    Game = application.Game,
                    Status = application.Status,
                    AppliedAt = application.AppliedAt,
                    ReviewedAt = application.ReviewedAt,
                    RejectionReason = application.RejectionReason,
                    ReviewedById = application.ReviewedById,
                    AppliedByCaptainId = application.AppliedByCaptainId
                });
            }

            return Ok(response);
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Player")]
        public async Task<IActionResult> WithdrawApplication(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null || !Guid.TryParse(userId, out var captainId))
                return Unauthorized();

            var application = await _context.Applications
                .FirstOrDefaultAsync(a => a.Id == id && a.AppliedByCaptainId == captainId);

            if (application == null)
                return NotFound("Application not found");

            if (application.Status != ApplicationStatus.Pending)
                return BadRequest($"Cannot withdraw application with status '{application.Status}'");

            var oldStatus = application.Status.ToString();
            application.Status = ApplicationStatus.Withdrawn;
            application.ReviewedAt = DateTime.UtcNow;
            application.RejectionReason = "Withdrawn by captain";

            await _context.SaveChangesAsync();

            // Публикуем событие через MassTransit
            await _publishEndpoint.Publish(new ApplicationStatusChangedEvent
            {
                ApplicationId = application.Id,
                TournamentId = application.TournamentId,
                TeamId = application.TeamId,
                OldStatus = oldStatus,
                NewStatus = application.Status.ToString(),
                Reason = "Withdrawn by captain",
                ChangedAt = DateTime.UtcNow
            });

            _logger.LogInformation("Application {ApplicationId} withdrawn by captain {CaptainId}",
                application.Id, captainId);

            return Ok("Application withdrawn successfully");
        }

        [HttpGet("tournament/{tournamentId:guid}")]
        [Authorize(Roles = "Organizer,Admin")]
        public async Task<IActionResult> GetTournamentApplications(Guid tournamentId)
        {
            // Сначала получаем все заявки
            var applications = await _context.Applications
                .Where(a => a.TournamentId == tournamentId)
                .OrderByDescending(a => a.AppliedAt)
                .ToListAsync();

            // Затем преобразуем в DTO с асинхронными вызовами
            var tournamentTitle = await GetTournamentTitle(tournamentId);
            var applicationResponses = new List<ApplicationResponse>();

            foreach (var application in applications)
            {
                applicationResponses.Add(new ApplicationResponse
                {
                    Id = application.Id,
                    TournamentId = application.TournamentId,
                    TournamentTitle = tournamentTitle,
                    TeamId = application.TeamId,
                    TeamName = application.TeamName,
                    Game = application.Game,
                    Status = application.Status,
                    AppliedAt = application.AppliedAt,
                    ReviewedAt = application.ReviewedAt,
                    RejectionReason = application.RejectionReason,
                    ReviewedById = application.ReviewedById,
                    AppliedByCaptainId = application.AppliedByCaptainId
                });
            }

            var tournamentStatus = await GetTournamentStatus(tournamentId);
            var response = new TournamentApplicationsResponse
            {
                TournamentId = tournamentId,
                TournamentTitle = tournamentTitle,
                TournamentStatus = tournamentStatus,
                Applications = applicationResponses,
                TotalPending = applicationResponses.Count(a => a.Status == ApplicationStatus.Pending),
                TotalApproved = applicationResponses.Count(a => a.Status == ApplicationStatus.Approved),
                TotalRejected = applicationResponses.Count(a => a.Status == ApplicationStatus.Rejected),
                TotalCancelled = applicationResponses.Count(a => a.Status == ApplicationStatus.Cancelled),
                TotalWithdrawn = applicationResponses.Count(a => a.Status == ApplicationStatus.Withdrawn)
            };

            return Ok(response);
        }

        [HttpGet("tournament/{tournamentId:guid}/info")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTournamentInfo(Guid tournamentId)
        {
            try
            {
                var tournamentInfo = await GetTournamentInfoFromService(tournamentId);
                if (tournamentInfo == null)
                    return NotFound("Tournament not found");

                // Сначала получаем все заявки
                var applications = await _context.Applications
                    .Where(a => a.TournamentId == tournamentId)
                    .OrderBy(a => a.AppliedAt)
                    .ToListAsync();

                // Затем преобразуем в DTO
                var applicationResponses = applications.Select(a => new ApplicationResponse
                {
                    Id = a.Id,
                    TournamentId = a.TournamentId,
                    TournamentTitle = tournamentInfo.Title,
                    TeamId = a.TeamId,
                    TeamName = a.TeamName,
                    Game = a.Game,
                    Status = a.Status,
                    AppliedAt = a.AppliedAt,
                    ReviewedAt = a.ReviewedAt,
                    RejectionReason = a.RejectionReason,
                    ReviewedById = a.ReviewedById,
                    AppliedByCaptainId = a.AppliedByCaptainId
                }).ToList();

                var response = new TournamentInfoResponse
                {
                    TournamentId = tournamentId,
                    Title = tournamentInfo.Title,
                    Description = tournamentInfo.Description ?? string.Empty,
                    Game = tournamentInfo.Game,
                    Status = tournamentInfo.Status,
                    MaxTeams = tournamentInfo.MaxTeams,
                    CurrentTeams = tournamentInfo.CurrentTeams,
                    ApprovedApplicationsCount = applicationResponses.Count(a => a.Status == ApplicationStatus.Approved),
                    ApprovedApplications = applicationResponses.Where(a => a.Status == ApplicationStatus.Approved).ToList(),
                    PendingApplications = applicationResponses.Where(a => a.Status == ApplicationStatus.Pending).ToList()
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tournament info for {TournamentId}", tournamentId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("{id:guid}/review")]
        [Authorize(Roles = "Organizer,Admin")]
        public async Task<IActionResult> ReviewApplication(
            Guid id,
            [FromBody] ReviewApplicationRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null || !Guid.TryParse(userId, out var reviewerId))
                return Unauthorized();

            var application = await _context.Applications
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null)
                return NotFound("Application not found");

            if (application.Status != ApplicationStatus.Pending)
                return BadRequest($"Cannot review application with status '{application.Status}'");

            // Проверяем, не превышен ли лимит команд (только при одобрении)
            if (request.IsApproved)
            {
                var approvedCount = await _context.Applications
                    .CountAsync(a => a.TournamentId == application.TournamentId &&
                                    a.Status == ApplicationStatus.Approved);

                var maxTeams = await GetTournamentMaxTeams(application.TournamentId);
                if (approvedCount >= maxTeams)
                    return BadRequest("Tournament has reached maximum team limit");
            }

            var oldStatus = application.Status.ToString();

            if (request.IsApproved)
            {
                application.Status = ApplicationStatus.Approved;
            }
            else
            {
                application.Status = ApplicationStatus.Rejected;
                application.RejectionReason = request.Reason;
            }

            application.ReviewedAt = DateTime.UtcNow;
            application.ReviewedById = reviewerId;

            await _context.SaveChangesAsync();

            // Публикуем событие через MassTransit
            await _publishEndpoint.Publish(new ApplicationStatusChangedEvent
            {
                ApplicationId = application.Id,
                TournamentId = application.TournamentId,
                TeamId = application.TeamId,
                OldStatus = oldStatus,
                NewStatus = application.Status.ToString(),
                Reason = request.Reason,
                ChangedAt = DateTime.UtcNow
            });

            _logger.LogInformation("Application {ApplicationId} {Action} by reviewer {ReviewerId}",
                application.Id, request.IsApproved ? "approved" : "rejected", reviewerId);

            return Ok(new
            {
                Message = $"Application {application.Status.ToString().ToLower()} successfully",
                ApplicationId = application.Id,
                Status = application.Status
            });
        }

        [HttpGet("stats/{tournamentId:guid}")]
        [Authorize(Roles = "Organizer,Admin")]
        public async Task<IActionResult> GetApplicationStats(Guid tournamentId)
        {
            var stats = await _context.Applications
                .Where(a => a.TournamentId == tournamentId)
                .GroupBy(a => a.Status)
                .Select(g => new
                {
                    Status = g.Key.ToString(),
                    Count = g.Count()
                })
                .ToListAsync();

            return Ok(stats);
        }


        private async Task<bool> ValidateTeamAndCaptain(Guid teamId, Guid captainId)
        {
            await Task.Delay(10);
            return true;
        }

        private async Task<bool> ValidateTournament(Guid tournamentId)
        {
            await Task.Delay(10);
            return true;
        }

        private async Task<bool> IsTournamentActive(Guid tournamentId)
        {
            await Task.Delay(10);
            return true;
        }

        private async Task<string> GetTeamName(Guid teamId)
        {
            await Task.Delay(10);
            return $"Team-{teamId.ToString().Substring(0, 8)}";
        }

        private async Task<string> GetTournamentGame(Guid tournamentId)
        {
            await Task.Delay(10);
            return "Dota2";
        }

        private async Task<string> GetTournamentTitle(Guid tournamentId)
        {
            await Task.Delay(10);
            return $"Tournament-{tournamentId.ToString().Substring(0, 8)}";
        }

        private async Task<string> GetTournamentStatus(Guid tournamentId)
        {
            await Task.Delay(10);
            return "Registration";
        }

        private async Task<int> GetTournamentMaxTeams(Guid tournamentId)
        {
            await Task.Delay(10);
            return 16;
        }

        private async Task<TournamentInfo?> GetTournamentInfoFromService(Guid tournamentId)
        {
            await Task.Delay(10);
            return new TournamentInfo
            {
                Title = $"Tournament-{tournamentId.ToString().Substring(0, 8)}",
                Description = "Demo tournament description",
                Game = "Dota2",
                Status = "Registration",
                MaxTeams = 16,
                CurrentTeams = 8
            };
        }

        private class TournamentInfo
        {
            public string Title { get; set; } = string.Empty;
            public string? Description { get; set; }
            public string Game { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public int MaxTeams { get; set; }
            public int CurrentTeams { get; set; }
        }
    }
}