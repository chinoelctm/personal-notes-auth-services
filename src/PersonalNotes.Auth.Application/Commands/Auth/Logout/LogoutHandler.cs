using MediatR;
using Microsoft.Extensions.Logging;
using PersonalNotes.Auth.Application.Helpers;
using PersonalNotes.Auth.Domain.Repositories;
using PersonalNotes.Auth.Domain.Services;

namespace PersonalNotes.Auth.Application.Commands.Auth.Logout
{
    /// <summary>
    /// Revoca el refresh token. Es idempotente: sin cookie o con un token desconocido no falla, porque el
    /// endpoint igual borra las cookies y el usuario queda fuera.
    /// </summary>
    public sealed class LogoutHandler : IRequestHandler<LogoutCommand>
    {
        private readonly ILogger<LogoutHandler> _logger;
        private readonly IRefreshSessionRepository _refreshRepo;
        private readonly IPasswordHashingService _passwordHasher;
        private readonly TimeHelper _timeHelper;

        public LogoutHandler(ILogger<LogoutHandler> logger, IRefreshSessionRepository refreshRepo,
            IPasswordHashingService passwordHasher, TimeHelper timeHelper)
        {
            _logger = logger;
            _refreshRepo = refreshRepo;
            _passwordHasher = passwordHasher;
            _timeHelper = timeHelper;
        }

        public async Task Handle(LogoutCommand command, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(command.RefreshToken))
                return;

            var refreshSession = await _refreshRepo.GetAsync(_passwordHasher.HashToken(command.RefreshToken), ct);
            if (refreshSession is null || refreshSession.IsRevoked())
            {
                _logger.LogInformation("Logout con un refresh token desconocido o ya revocado.");
                return;
            }

            refreshSession.Revoke(_timeHelper.GetUtcNow());
            await _refreshRepo.UpdateAsync(refreshSession, ct);

            _logger.LogInformation("Sesión cerrada. UserId: {UserId}", refreshSession.UserId);
        }
    }
}
