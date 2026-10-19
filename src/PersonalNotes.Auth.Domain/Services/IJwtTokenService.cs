using PersonalNotes.Auth.Domain.Entities;

namespace PersonalNotes.Auth.Domain.Services
{
    public interface IJwtTokenService
    {
        string GenerateAccessToken(User user, int expirationMinutes);
        (string plainToken, string tokenHash) GenerateRefreshToken();
    }
}
