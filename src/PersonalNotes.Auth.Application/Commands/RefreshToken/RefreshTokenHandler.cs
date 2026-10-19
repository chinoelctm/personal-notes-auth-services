using MediatR;
using Microsoft.Extensions.Logging;
using PersonalNotes.Auth.Application.Helpers;
using PersonalNotes.Auth.Application.Services.Tokens;
using PersonalNotes.Auth.Domain.Exceptions;
using PersonalNotes.Auth.Domain.Repositories;
using PersonalNotes.Auth.Domain.Services;

namespace PersonalNotes.Auth.Application.Commands.RefreshToken
{
    /// <summary>Rotación: el refresh token recibido queda usado y se emiten un access token y un refresh token nuevos.</summary>
    public sealed class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, TokenIssueResult>
    {
        private readonly ILogger<RefreshTokenHandler> _logger;
        private readonly IPasswordHashingService _passwordHasher;
        private readonly IRefreshSessionRepository _repository;
        private readonly IUserRepository _userRepository;
        private readonly ITokenIssuer _tokenIssuer;
        private readonly TimeHelper _timeHelper;

        public RefreshTokenHandler(ILogger<RefreshTokenHandler> logger, IRefreshSessionRepository repository,
            IUserRepository userRepository, TimeHelper timeHelper, IPasswordHashingService passwordHasher,
            ITokenIssuer tokenIssuer)
        {
            _logger = logger;
            _repository = repository;
            _userRepository = userRepository;
            _timeHelper = timeHelper;
            _passwordHasher = passwordHasher;
            _tokenIssuer = tokenIssuer;
        }

        public async Task<TokenIssueResult> Handle(RefreshTokenCommand command, CancellationToken ct)
        {
            var now = _timeHelper.GetUtcNow();
            var storedToken = await _repository.GetAsync(_passwordHasher.HashToken(command.RefreshToken), ct);

            if (storedToken is null)
            {
                _logger.LogWarning("Intento de refresh con un token no encontrado.");
                throw new InvalidRefreshTokenException();
            }
            if (storedToken.IsUsed)
            {
                _logger.LogWarning("Refresh token ya utilizado. UserId: {UserId}", storedToken.UserId);
                throw new InvalidRefreshTokenException();
            }
            if (storedToken.IsRevoked())
            {
                _logger.LogWarning("Refresh token revocado. UserId: {UserId}", storedToken.UserId);
                throw new InvalidRefreshTokenException();
            }
            if (storedToken.IsExpired(now))
            {
                _logger.LogWarning("Refresh token expirado. UserId: {UserId}", storedToken.UserId);
                throw new InvalidRefreshTokenException();
            }

            var user = await _userRepository.GetByIdAsync(storedToken.UserId, ct);
            if (user is null || !user.IsActive)
            {
                _logger.LogWarning("Usuario {UserId} no existe o se encuentra deshabilitado.", storedToken.UserId);
                throw new InvalidRefreshTokenException();
            }

            storedToken.MarkAsUsed(now);
            await _repository.UpdateAsync(storedToken, ct);

            _logger.LogInformation("Refresh token rotado. UserId: {UserId}", user.Id);

            return await _tokenIssuer.IssueAsync(user, ct);
        }
    }
}
