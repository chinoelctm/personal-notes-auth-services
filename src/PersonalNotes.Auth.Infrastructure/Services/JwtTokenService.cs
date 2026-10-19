using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using PersonalNotes.Auth.Domain.Entities;
using PersonalNotes.Auth.Domain.Options;
using PersonalNotes.Auth.Domain.Services;

namespace PersonalNotes.Auth.Infrastructure.Services
{
    public sealed class JwtTokenService : IJwtTokenService
    {
        private readonly IJwtOptions _settings;
        private readonly IPasswordHashingService _passwordHashingService;
        private readonly RsaSecurityKey _rsaSecurityKey;

        public JwtTokenService(IJwtOptions settings, IPasswordHashingService passwordHashingService)
        {
            _settings = settings;
            _passwordHashingService = passwordHashingService;
            _rsaSecurityKey = new RsaSecurityKey(PemKeys.ImportRsa(_settings.PrivateKeyPem, requirePrivateKey: true));
        }

        public string GenerateAccessToken(User user, int expirationMinutes)
        {
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.UniqueName, user.Username),
                new("name", user.Name),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var credentials = new SigningCredentials(_rsaSecurityKey, SecurityAlgorithms.RsaSha256);

            var token = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public (string plainToken, string tokenHash) GenerateRefreshToken()
        {
            var randomBytes = new byte[32];
            RandomNumberGenerator.Fill(randomBytes);

            var plainToken = Convert.ToBase64String(randomBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');

            return (plainToken, _passwordHashingService.HashToken(plainToken));
        }
    }
}
