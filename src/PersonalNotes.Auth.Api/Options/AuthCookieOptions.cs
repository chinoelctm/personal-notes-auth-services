using System.ComponentModel.DataAnnotations;

namespace PersonalNotes.Auth.Api.Options
{
    /// <summary>
    /// Mismas cookies que el BFF de WebInsights: auth_token (en todo el sitio) y refresh_token (solo en las
    /// rutas de auth, así no viaja en cada request a la API de notas). Ambas HttpOnly: el JavaScript del
    /// front nunca ve los tokens.
    /// </summary>
    public class AuthCookieOptions
    {
        public const string SectionName = "AuthCookies";

        [Required] public string AccessTokenName { get; set; } = "auth_token";
        [Required] public string RefreshTokenName { get; set; } = "refresh_token";
        [Required] public string RefreshTokenPath { get; set; } = "/api/v1/auth";

        /// <summary>Solo HTTPS. En desarrollo (http://localhost) va en false.</summary>
        public bool Secure { get; set; } = true;
    }
}
