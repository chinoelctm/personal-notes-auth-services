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
            _rsaSecurityKey = CreateRsaSecurityKey(_settings.PrivateKeyPem);
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

        private static RsaSecurityKey CreateRsaSecurityKey(string privateKeyPem)
        {
            if (string.IsNullOrWhiteSpace(privateKeyPem))
                throw new InvalidOperationException("La clave privada JWT no está configurada.");

            var rsa = RSA.Create();
            // Render y los .env guardan el PEM en una línea con "\n" literales
            rsa.ImportFromPem(privateKeyPem.Replace("\\n", "\n"));

            return new RsaSecurityKey(rsa);
        }
    }
}
