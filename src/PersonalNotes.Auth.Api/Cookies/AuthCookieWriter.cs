using Microsoft.Extensions.Options;
using PersonalNotes.Auth.Api.Options;
using PersonalNotes.Auth.Application.Services.Tokens;

namespace PersonalNotes.Auth.Api.Cookies
{
    /// <summary>
    /// Escribe y borra las cookies de sesión. SameSite=Strict: el front llama a la API por el mismo origen
    /// (rewrite de Render / proxy de ng serve), así que las cookies nunca se envían desde otros sitios (CSRF).
    /// </summary>
    public class AuthCookieWriter
    {
        private readonly AuthCookieOptions _options;

        public AuthCookieWriter(IOptions<AuthCookieOptions> options)
        {
            _options = options.Value;
        }

        public string AccessTokenName => _options.AccessTokenName;
        public string RefreshTokenName => _options.RefreshTokenName;

        public void Write(HttpResponse response, TokenIssueResult tokens)
        {
            response.Cookies.Append(_options.AccessTokenName, tokens.AccessToken,
                Build("/", TimeSpan.FromSeconds(tokens.AccessTokenExpiresIn)));
            response.Cookies.Append(_options.RefreshTokenName, tokens.RefreshToken,
                Build(_options.RefreshTokenPath, TimeSpan.FromSeconds(tokens.RefreshTokenExpiresIn)));
        }

        public void Clear(HttpResponse response)
        {
            response.Cookies.Delete(_options.AccessTokenName, Build("/", null));
            response.Cookies.Delete(_options.RefreshTokenName, Build(_options.RefreshTokenPath, null));
        }

        private CookieOptions Build(string path, TimeSpan? maxAge) => new()
        {
            HttpOnly = true,
            Secure = _options.Secure,
            SameSite = SameSiteMode.Strict,
            Path = path,
            MaxAge = maxAge,
            IsEssential = true
        };
    }
}
