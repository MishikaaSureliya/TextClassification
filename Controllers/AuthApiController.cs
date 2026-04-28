using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TextClassification.Data;
using TextClassification.DTOs;
using TextClassification.Models;

namespace TextClassification.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthApiController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthApiController> _logger;

        public AuthApiController(AppDbContext context, IConfiguration config, ILogger<AuthApiController> logger)
        {
            _context = context;
            _config = config;
            _logger = logger;
        }

        // ================= REGISTER =================
        [HttpPost("register")]
        public IActionResult Register(RegisterDto dto)
        {
            try
            {
                _logger.LogInformation("Register attempt for email: {Email}", dto.Email);

                // 🔥 VALIDATIONS
                if (!System.Text.RegularExpressions.Regex.IsMatch(dto.Name, @"^[A-Za-z]+$"))
                {
                    _logger.LogWarning("Invalid name format: {Name}", dto.Name);
                    return BadRequest("Name must contain only characters");
                }

                if (!dto.Email.Contains("@"))
                {
                    _logger.LogWarning("Invalid email: {Email}", dto.Email);
                    return BadRequest("Invalid email");
                }

                if (dto.Password != dto.ConfirmPassword)
                {
                    _logger.LogWarning("Password mismatch for email: {Email}", dto.Email);
                    return BadRequest("Passwords do not match");
                }

                if (!System.Text.RegularExpressions.Regex.IsMatch(dto.Password,
                    @"^(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{8,}$"))
                {
                    _logger.LogWarning("Weak password attempt for email: {Email}", dto.Email);
                    return BadRequest("Password must be 8 chars with 1 capital, 1 number, 1 special char");
                }

                // 🔥 DUPLICATE CHECK
                if (_context.Users.Any(x => x.Email == dto.Email))
                {
                    _logger.LogWarning("Duplicate email registration attempt: {Email}", dto.Email);
                    return BadRequest("Email already registered");
                }

                if (_context.Users.Any(x => x.Name == dto.Name))
                {
                    _logger.LogWarning("Duplicate username attempt: {Name}", dto.Name);
                    return BadRequest("Username already taken");
                }

                var hasher = new PasswordHasher<User>();

                var user = new User
                {
                    Name = dto.Name,
                    Email = dto.Email,
                    CreatedBy = dto.Name
                };

                // 🔐 HASH PASSWORD
                user.PasswordHash = hasher.HashPassword(user, dto.Password);

                _context.Users.Add(user);
                _context.SaveChanges();

                _logger.LogInformation("User registered successfully: {Email}", dto.Email);

                return Ok("Registered successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                return StatusCode(500, "Something went wrong");
            }
        }

        // ================= LOGIN =================
        [HttpPost("login")]
        public IActionResult Login(LoginDto dto)
        {
            try
            {
                _logger.LogInformation("Login attempt: {Email}", dto.Email);

                var user = _context.Users.FirstOrDefault(x => x.Email == dto.Email);

                if (user == null)
                {
                    _logger.LogWarning("Login failed - user not found: {Email}", dto.Email);
                    return Unauthorized("Invalid credentials");
                }

                var hasher = new PasswordHasher<User>();
                var result = hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);

                if (result == PasswordVerificationResult.Failed)
                {
                    _logger.LogWarning("Login failed - wrong password: {Email}", dto.Email);
                    return Unauthorized("Invalid credentials");
                }

                var token = GenerateToken(user);

                _logger.LogInformation("Login successful: {Email}", dto.Email);

                return Ok(new { token });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return StatusCode(500, "Something went wrong");
            }
        }

        // ================= TOKEN =================
        private string GenerateToken(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(2),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}