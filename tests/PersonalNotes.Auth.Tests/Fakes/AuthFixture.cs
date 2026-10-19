using System.Security.Cryptography;
using Microsoft.Extensions.Logging.Abstractions;
using PersonalNotes.Auth.Application.Commands.Auth.Login;
using PersonalNotes.Auth.Application.Commands.RefreshToken;
using PersonalNotes.Auth.Application.Helpers;
using PersonalNotes.Auth.Application.Services.Auth;
using PersonalNotes.Auth.Application.Services.Tokens;
using PersonalNotes.Auth.Domain.Entities;
using PersonalNotes.Auth.Infrastructure.Options;
using PersonalNotes.Auth.Infrastructure.Services;

namespace PersonalNotes.Auth.Tests.Fakes
{
    /// <summary>Arma los handlers reales con repositorios en memoria, BCrypt real y una clave RSA de prueba.</summary>
    public class AuthFixture
    {
        public const string Username = "JoaquinMarquez";
        public const string Password = "contraseña-de-prueba";

        public FakeTimeHelper Time { get; } = new(new DateTime(2026, 10, 19, 12, 0, 0, DateTimeKind.Utc));
        public InMemoryUserRepository Users { get; } = new();
        public InMemoryRefreshSessionRepository Sessions { get; } = new();
        public PasswordHashingService Hasher { get; } = new();
        public RSA Rsa { get; } = RSA.Create(2048);
        public JwtOptions Jwt { get; }
        public LoginOptions Login { get; } = new() { MaxAttempts = 3, LockDurationMinutes = 15 };
        public JwtTokenService JwtService { get; }
        public User User { get; }

        public AuthFixture()
        {
            Jwt = new JwtOptions
            {
                PrivateKeyPem = Rsa.ExportPkcs8PrivateKeyPem(),
                Issuer = "PersonalNotes.Auth",
                Audience = "PersonalNotes",
                AccessTokenMinutes = 15,
                RefreshTokenDays = 7
            };
            JwtService = new JwtTokenService(Jwt, Hasher);

            User = User.Create(Username, "Joaquín", Hasher.HashPassword(Password), Time.GetUtcNow());
            Users.Users.Add(User);
        }

        public TokenIssuer TokenIssuer()
            => new(NullLogger<TokenIssuer>.Instance, JwtService, Sessions, Jwt, Time);

        public LoginHandler LoginHandler()
            => new(NullLogger<LoginHandler>.Instance,
                new CredentialValidator(NullLogger<CredentialValidator>.Instance, Users, Hasher, Login, Time),
                TokenIssuer());

        public RefreshTokenHandler RefreshHandler()
            => new(NullLogger<RefreshTokenHandler>.Instance, Sessions, Users, Time, Hasher, TokenIssuer());
    }

    public class FakeTimeHelper(DateTime utcNow) : TimeHelper(TimeZoneInfo.Utc)
    {
        public DateTime UtcNow { get; set; } = utcNow;

        public override DateTime GetUtcNow() => UtcNow;
    }
}
