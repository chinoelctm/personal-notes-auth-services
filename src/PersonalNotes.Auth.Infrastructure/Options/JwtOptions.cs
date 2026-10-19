using System.ComponentModel.DataAnnotations;
using PersonalNotes.Auth.Domain.Options;

namespace PersonalNotes.Auth.Infrastructure.Options
{
    public class JwtOptions : IJwtOptions
    {
        public const string SectionName = "JwtSettings";

        /// <summary>Clave RSA privada (PKCS#8 PEM) que firma los access tokens (RS256). Nunca en el repo.</summary>
        [Required(ErrorMessage = "Falta JwtSettings:PrivateKeyPem (user-secrets en desarrollo, variable JwtSettings__PrivateKeyPem en Render).")]
        public string PrivateKeyPem { get; set; } = null!;

        [Required] public string Issuer { get; set; } = null!;
        [Required] public string Audience { get; set; } = null!;

        [Range(1, 15, ErrorMessage = "El access token no puede durar más de 15 minutos.")]
        public int AccessTokenMinutes { get; set; } = 15;

        [Range(1, 7, ErrorMessage = "El refresh token no puede durar más de 7 días.")]
        public int RefreshTokenDays { get; set; } = 7;
    }
}
