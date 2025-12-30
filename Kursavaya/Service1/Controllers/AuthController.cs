using AuthService.Model;
using AuthService.Models;
using AuthService.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AuthService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly RoleManager<ApplicationRole> _roleManager;
        
        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IJwtTokenService jwtTokenService,
            RoleManager<ApplicationRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtTokenService = jwtTokenService;
            _roleManager = roleManager;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            if (!await _roleManager.RoleExistsAsync(request.Role))
            {
                return BadRequest($"Role '{request.Role}' does not exist");
            }

            var existingEmail = await _userManager.FindByEmailAsync(request.Email);
            if (existingEmail != null)
            {
                return BadRequest("Email is already registered");
            }

            string finalUserName = request.UserName;
            if (request.Role == RoleNames.Organizer && !finalUserName.StartsWith("org_"))
            {
                finalUserName = $"org_{finalUserName}";
            }

            var existingUserName = await _userManager.FindByNameAsync(finalUserName);
            if (existingUserName != null)
            {
                return BadRequest($"Username '{finalUserName}' is already taken");
            }

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = finalUserName, 
                Email = request.Email,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            await _userManager.AddToRoleAsync(user, request.Role);

            var token = await _jwtTokenService.GenerateTokenAsync(user);

            return Ok(new AuthResponse
            {
                Token = token,
                UserId = user.Id,
                Email = user.Email,
                UserName = user.UserName, // Возвращаем UserName (уже с префиксом если организатор)
                Role = request.Role
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user == null)
            {
                return Unauthorized("Invalid credentials");
            }

            var result = await _signInManager.CheckPasswordSignInAsync(
                user,
                request.Password,
                lockoutOnFailure: false);

            if (!result.Succeeded)
            {
                return Unauthorized("Invalid credentials");
            }

            var token = await _jwtTokenService.GenerateTokenAsync(user);
            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new AuthResponse
            {
                Token = token,
                UserId = user.Id,
                Email = user.Email,
                UserName = user.UserName!, // Возвращаем UserName
                Role = roles.FirstOrDefault()
            });
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return Unauthorized();
            }

            var result = await _userManager.ChangePasswordAsync(
                user,
                request.CurrentPassword,
                request.NewPassword);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            return Ok("Password changed successfully");
        }
        [HttpGet("check-playertag/{playerTag}")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckPlayerTagAvailability(string playerTag)
        {
            if (string.IsNullOrWhiteSpace(playerTag))
            {
                return BadRequest("PlayerTag is required");
            }

            var existing = await _userManager.Users
                .FirstOrDefaultAsync(u => u.PlayerTag == playerTag);

            return Ok(new
            {
                available = existing == null
            });
        }
    }
}