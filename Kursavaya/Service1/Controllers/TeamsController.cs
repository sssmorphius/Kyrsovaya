using AuthService.Dto;
using AuthService.Model;
using AuthService.Models;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts.Events;
using System.Security.Claims;

namespace AuthService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TeamsController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AuthDbContext _context;
        private readonly IPublishEndpoint _publishEndpoint; 
        private readonly ILogger<TeamsController> _logger;

        public TeamsController(
            UserManager<ApplicationUser> userManager,
            AuthDbContext context,
            IPublishEndpoint publishEndpoint, 
            ILogger<TeamsController> logger)
        {
            _userManager = userManager;
            _context = context;
            _publishEndpoint = publishEndpoint;
            _logger = logger;
        }

        [HttpPost]
        [Authorize(Roles = "Player")]
        public async Task<IActionResult> CreateTeam([FromBody] CreateTeamRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("User not found");

            var existing = await _context.Teams
                .AnyAsync(t => t.Name == request.Name || t.Tag == request.Tag);
            if (existing) return BadRequest("Team name or tag already exists");

            if (request.Game != "Dota2" && request.Game != "CS2")
                return BadRequest("Game must be 'Dota2' or 'CS2'");

            var team = new Team
            {
                Name = request.Name,
                Tag = request.Tag,
                Game = request.Game,
                CaptainId = Guid.Parse(userId),
                LogoUrl = request.LogoUrl,
                CreatedAt = DateTime.UtcNow
            };

            var teamMember = new TeamMember
            {
                Team = team,
                User = user,
                Role = "Captain",
                JoinedAt = DateTime.UtcNow
            };

            _context.Teams.Add(team);
            _context.TeamMembers.Add(teamMember);

            try
            {
                await _context.SaveChangesAsync();
                await _publishEndpoint.Publish(new TeamCreatedEvent
                {
                    TeamId = team.Id,
                    TeamName = team.Name,
                    Game = team.Game,
                    CaptainId = team.CaptainId,
                    CreatedAt = DateTime.UtcNow
                });
                return Ok(new { team.Id, message = "Team created successfully" });
                _logger.LogInformation("Team created: {TeamName} ({TeamId})", team.Name, team.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating team");
                return StatusCode(500, $"Error creating team: {ex.Message}");
            }
        }

        [HttpPost("{teamId:guid}/players")]
        public async Task<IActionResult> AddPlayer(
            Guid teamId,
            [FromBody] AddPlayerToTeamRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var team = await _context.Teams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.Id == teamId);

            if (team == null) return NotFound("Team not found");

            if (team.CaptainId != Guid.Parse(userId))
                return Forbid("Only team captain can add players");

            var playerToAdd = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (playerToAdd == null) return NotFound("User not found");

            var isPlayer = await _userManager.IsInRoleAsync(playerToAdd, "Player");
            if (!isPlayer) return BadRequest("User must have Player role");

            var alreadyMember = team.Members.Any(m => m.UserId == request.UserId);
            if (alreadyMember) return BadRequest("Player is already in the team");

            var currentMembersCount = team.Members.Count;
            if (currentMembersCount >= 5)
                return BadRequest("Team already has 5 players (maximum for Dota2/CS2)");

            var teamMember = new TeamMember
            {
                TeamId = teamId,
                UserId = request.UserId,
                Role = "Member",
                JoinedAt = DateTime.UtcNow
            };

            _context.TeamMembers.Add(teamMember);
            await _context.SaveChangesAsync();

            return Ok($"Player {playerToAdd.UserName} added to team");
        }

        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTeam(Guid id)
        {
            var team = await _context.Teams
                .Include(t => t.Captain)
                .Include(t => t.Members)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (team == null) return NotFound("Team not found");

            var response = new TeamResponse
            {
                Id = team.Id,
                Name = team.Name,
                Tag = team.Tag,
                Game = team.Game,
                CaptainId = team.CaptainId,
                CaptainName = team.Captain.PlayerTag ?? team.Captain.UserName!,
                LogoUrl = team.LogoUrl,
                CreatedAt = team.CreatedAt,
                Members = team.Members.Select(m => new TeamMemberResponse
                {
                    UserId = m.UserId,
                    UserName = m.User.UserName!,
                    Email = m.User.Email!,
                    Role = m.Role,
                    JoinedAt = m.JoinedAt
                }).ToList()
            };

            return Ok(response);
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMyTeams()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var userTeams = await _context.TeamMembers
                .Include(tm => tm.Team)
                .Where(tm => tm.UserId == Guid.Parse(userId))
                .Select(tm => new
                {
                    tm.Team.Id,
                    tm.Team.Name,
                    tm.Team.Tag,
                    tm.Team.Game,
                    tm.Role,
                    IsCaptain = tm.Team.CaptainId == tm.UserId
                })
                .ToListAsync();

            return Ok(userTeams);
        }

        [HttpDelete("{teamId:guid}/players/{playerId:guid}")]
        public async Task<IActionResult> RemovePlayer(Guid teamId, Guid playerId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var team = await _context.Teams
                    .Include(t => t.Members) 
                    .FirstOrDefaultAsync(t => t.Id == teamId);
            if (team == null) return NotFound("Team not found");

            if (team.CaptainId != Guid.Parse(userId))
                return Forbid("Only team captain can remove players");

            if (playerId == team.CaptainId)
                return BadRequest("Cannot remove captain from team");

            var teamMember = await _context.TeamMembers
                .FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.UserId == playerId);

            if (teamMember == null) return NotFound("Player not found in team");

            _context.TeamMembers.Remove(teamMember);
            await _context.SaveChangesAsync();

            if (team != null)
            {
                await _publishEndpoint.Publish<TeamMemberChangedEvent>(new
                {
                    TeamId = team.Id,
                    MemberCount = team.Members.Count,
                    UpdatedAt = DateTime.UtcNow
                });

                _logger.LogInformation("Team {TeamId} member count updated: {Count}",
                    team.Id, team.Members.Count);
            }

            return Ok("Player removed from team");
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllTeams()
        {
            var teams = await _context.Teams
                .Include(t => t.Captain)
                .Select(t => new
                {
                    t.Id,
                    t.Name,
                    t.Tag,
                    t.Game,
                    CaptainName = t.Captain.PlayerTag ?? t.Captain.UserName,
                    MemberCount = t.Members.Count,
                    t.CreatedAt
                })
                .ToListAsync();

            return Ok(teams);
        }
    }
}
