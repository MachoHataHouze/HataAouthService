// Services/AuthService.cs
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using HataAuthService.Models;
using HataAuthService.Repositories;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace HataAuthService.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IUserRepository userRepository, IConfiguration configuration, HttpClient httpClient, ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _configuration = configuration;
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task RegisterAsync(User user, string password)
        {
            var existingUser = await _userRepository.GetByEmailAsync(user.Email);
            if (existingUser != null)
            {
                throw new ArgumentException("Email already exists.");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            user.Verified = true;
            user.CreatedAt = DateTime.UtcNow;

            await _userRepository.AddAsync(user);

            // Создаем профиль пользователя
            var profileDto = new ProfileDto
            {
                UserId = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                ContactInfo = "",
                ProfilePicture = ""
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/profile", profileDto);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to create user profile. Status Code: {StatusCode}, Content: {Content}", response.StatusCode, errorContent);
                    throw new Exception("Failed to create user profile.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating user profile.");
                throw;
            }
        }

        public async Task<string> AuthenticateAsync(string email, string password)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                throw new ArgumentException("Invalid email or password.");
            }

            if (!user.Verified)
            {
                throw new ArgumentException("Email not verified.");
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim("nameid", user.Id.ToString()), // Используем nameid для соответствия ожидаемому claim
                    new Claim(ClaimTypes.Email, user.Email)
                }),
                Expires = DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["Jwt:ExpiryMinutes"])),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"]
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

    }
}
