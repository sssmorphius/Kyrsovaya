using AuthService.Dto;
using AuthService.Model;
using AuthService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AuthService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AuthDbContext _context; 

        public ProfileController(
            UserManager<ApplicationUser> userManager,
            AuthDbContext context) 
        {
            _userManager = userManager;
            _context = context; 
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var user = await _context.Users
                .Include(u => u.TeamMemberships)
                    .ThenInclude(tm => tm.Team) 
                .FirstOrDefaultAsync(u => u.Id == Guid.Parse(userId));

            if (user == null) return NotFound("User not found");

            var roles = await _userManager.GetRolesAsync(user);

            var teamsInfo = user.TeamMemberships.Select(tm => new
            {
                TeamId = tm.Team.Id,
                TeamName = tm.Team.Name,
                TeamTag = tm.Team.Tag,
                Game = tm.Team.Game,
                RoleInTeam = tm.Role,
                IsCaptain = tm.Team.CaptainId == user.Id,
                JoinedAt = tm.JoinedAt
            }).ToList();

            return Ok(new
            {
                user.Id,
                user.Email,
                user.UserName,
                user.PlayerTag,
                user.AvatarUrl,
                user.CreatedAt,
                Roles = roles,
                Teams = teamsInfo
            });
        }

        [HttpPut("me/username")]
        public async Task<IActionResult> UpdateUserName([FromBody] string username)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("User not found");

            var roles = await _userManager.GetRolesAsync(user);
            var isOrganizer = roles.Contains(RoleNames.Organizer);

            string finalUserName = username;
            if (isOrganizer && !finalUserName.StartsWith("org_"))
            {
                finalUserName = $"org_{finalUserName}";
            }

            var existingUser = await _userManager.FindByNameAsync(finalUserName);
            if (existingUser != null && existingUser.Id != Guid.Parse(userId))
            {
                return BadRequest($"Username '{finalUserName}' is already taken");
            }

            user.UserName = finalUserName;
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok("Username updated");
        }

        [HttpPut("me/player-tag")]
        public async Task<IActionResult> UpdatePlayerTag([FromBody] string? playerTag)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("User not found");

            var isOrganizer = await _userManager.IsInRoleAsync(user, RoleNames.Organizer);

            if (isOrganizer)
            {
                if (string.IsNullOrEmpty(playerTag))
                    return BadRequest("Organizers must have a PlayerTag");

                if (!playerTag!.StartsWith("org_"))
                {
                    playerTag = $"org_{playerTag}";
                }

                var existingUser = await _userManager.Users
                    .FirstOrDefaultAsync(u => u.PlayerTag == playerTag && u.Id != Guid.Parse(userId));
                if (existingUser != null)
                    return BadRequest($"PlayerTag '{playerTag}' is already taken");
            }

            user.PlayerTag = playerTag; 
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new
            {
                Message = "Player tag updated",
                PlayerTag = playerTag
            });
        }

        [HttpPut("me/avatar")]
        public async Task<IActionResult> UpdateAvatar([FromBody] UpdateAvatarRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("User not found");

            // Простая проверка URL
            if (string.IsNullOrWhiteSpace(request.AvatarUrl))
                return BadRequest("AvatarUrl is required");

            user.AvatarUrl = request.AvatarUrl;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new
            {
                Message = "Avatar updated",
                AvatarUrl = request.AvatarUrl
            });
        }
    }
}