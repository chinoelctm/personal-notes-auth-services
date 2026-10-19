using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using PersonalNotes.Auth.Tests.Fakes;

namespace PersonalNotes.Auth.Tests.Infrastructure
{
    public class JwtTokenServiceTests
    {
        private static TokenValidationParameters ValidationWith(RSA publicKey, AuthFixture fx) => new()
        {
            ValidIssuer = fx.Jwt.Issuer,
            ValidAudience = fx.Jwt.Audience,
            IssuerSigningKey = new RsaSecurityKey(publicKey),
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
            ClockSkew = TimeSpan.Zero
        };

        [Fact]
        public void AccessToken_IsRs256_ValidatesWithThePublicKeyOnly_AndLastsFifteenMinutes()
        {
            var fx = new AuthFixture();
            var token = fx.JwtService.GenerateAccessToken(fx.User, fx.Jwt.AccessTokenMinutes);

            // personal-notes-service solo conoce la clave pública
            var publicOnly = RSA.Create();
            publicOnly.ImportFromPem(fx.Rsa.ExportSubjectPublicKeyInfoPem());

            var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
            handler.ValidateToken(token, ValidationWith(publicOnly, fx), out var validated);

            var jwt = (JwtSecurityToken)validated;
            Assert.Equal(SecurityAlgorithms.RsaSha256, jwt.Header.Alg);
            Assert.Equal(fx.User.Id.ToString(), jwt.Subject);
            Assert.InRange(jwt.ValidTo - jwt.ValidFrom, TimeSpan.FromMinutes(14), TimeSpan.FromMinutes(15));
        }

        [Fact]
        public void AccessToken_SignedWithAnotherKey_IsRejected()
        {
            var fx = new AuthFixture();
            var other = new AuthFixture();
            var token = other.JwtService.GenerateAccessToken(other.User, 15);

            var handler = new JwtSecurityTokenHandler();
            Assert.ThrowsAny<SecurityTokenException>(() =>
                handler.ValidateToken(token, ValidationWith(fx.Rsa, fx), out _));
        }

        [Fact]
        public void PrivateKeyWithEscapedNewlines_FromAnEnvironmentVariable_IsAccepted()
        {
            var fx = new AuthFixture();
            fx.Jwt.PrivateKeyPem = fx.Jwt.PrivateKeyPem.Replace("\n", "\\n");

            var service = new PersonalNotes.Auth.Infrastructure.Services.JwtTokenService(fx.Jwt, fx.Hasher);

            Assert.NotEmpty(service.GenerateAccessToken(fx.User, 15));
        }

        [Fact]
        public void RefreshTokens_AreRandom_AndOnlyTheirHashIsReturnedForStorage()
        {
            var fx = new AuthFixture();

            var (plain1, hash1) = fx.JwtService.GenerateRefreshToken();
            var (plain2, _) = fx.JwtService.GenerateRefreshToken();

            Assert.NotEqual(plain1, plain2);
            Assert.Equal(fx.Hasher.HashToken(plain1), hash1);
            Assert.True(plain1.Length >= 43);
        }
    }
}
