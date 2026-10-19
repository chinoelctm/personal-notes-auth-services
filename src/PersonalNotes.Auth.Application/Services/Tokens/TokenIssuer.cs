using Microsoft.Extensions.Logging;
using PersonalNotes.Auth.Application.Helpers;
using PersonalNotes.Auth.Domain.Entities;
using PersonalNotes.Auth.Domain.Options;
using PersonalNotes.Auth.Domain.Repositories;
using PersonalNotes.Auth.Domain.Services;

namespace PersonalNotes.Auth.Application.Services.Tokens
{
    /// <summary>Emite el access token (JWT RS256) y un refresh token opaco nuevo, guardando solo su hash.</summary>
    public class TokenIssuer : ITokenIssuer
    {
        private readonly ILogger<TokenIssuer> _logger;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IRefreshSessionRepository _refreshSessionRepository;
        private readonly IJwtOptions _jwtOptions;
        private readonly TimeHelper _timeHelper;

        public TokenIssuer(ILogger<TokenIssuer> logger, IJwtTokenService jwtTokenService,
            IRefreshSessionRepository refreshSessionRepository, IJwtOptions jwtOptions, TimeHelper timeHelper)
        {
            _logger = logger;
            _jwtTokenService = jwtTokenService;
            _refreshSessionRepository = refreshSessionRepository;
            _jwtOptions = jwtOptions;
            _timeHelper = timeHelper;
        }

        public async Task<TokenIssueResult> IssueAsync(User user, CancellationToken ct)
        {
            var accessToken = _jwtTokenService.GenerateAccessToken(user, _jwtOptions.AccessTokenMinutes);
            var (plainRefreshToken, refreshTokenHash) = _jwtTokenService.GenerateRefreshToken();

            var refreshSession = RefreshSession.Create(user.Id, refreshTokenHash, _timeHelper.GetUtcNow(),
                expirationDays: _jwtOptions.RefreshTokenDays);
            await _refreshSessionRepository.AddAsync(refreshSession, ct);

            _logger.LogInformation("Tokens emitidos. UserId: {UserId}", user.Id);

            return new TokenIssueResult(
                AccessToken: accessToken,
                RefreshToken: plainRefreshToken,
                AccessTokenExpiresIn: _jwtOptions.AccessTokenMinutes * 60,
                RefreshTokenExpiresIn: (int)TimeSpan.FromDays(_jwtOptions.RefreshTokenDays).TotalSeconds);
        }
    }
}
