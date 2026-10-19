using Microsoft.Extensions.Logging;
using PersonalNotes.Auth.Application.Helpers;
using PersonalNotes.Auth.Domain.Options;
using PersonalNotes.Auth.Domain.Repositories;
using PersonalNotes.Auth.Domain.Services;

namespace PersonalNotes.Auth.Application.Services.Auth
{
    public class CredentialValidator : ICredentialValidator
    {
        // Hash de una contraseña que nadie usa: si el usuario no existe se verifica igual, para que el tiempo
        // de respuesta no delate qué usernames existen
        private static string? _dummyHash;

        private readonly ILogger<CredentialValidator> _logger;
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHashingService _passwordHashingService;
        private readonly ILoginOptions _loginOptions;
        private readonly TimeHelper _timeHelper;

        public CredentialValidator(ILogger<CredentialValidator> logger, IUserRepository userRepository,
            IPasswordHashingService passwordHashingService, ILoginOptions loginOptions, TimeHelper timeHelper)
        {
            _logger = logger;
            _userRepository = userRepository;
            _passwordHashingService = passwordHashingService;
            _loginOptions = loginOptions;
            _timeHelper = timeHelper;
        }

        public async Task<CredentialValidationResult> ValidateAsync(string username, string password, CancellationToken ct)
        {
            var now = _timeHelper.GetUtcNow();
            var user = await _userRepository.GetByUsernameAsync(username, ct);

            if (user is null || !user.IsActive)
            {
                _dummyHash ??= _passwordHashingService.HashPassword(Guid.NewGuid().ToString());
                _passwordHashingService.VerifyPassword(password, _dummyHash);
                _logger.LogWarning("Credenciales inválidas: usuario no encontrado o deshabilitado. Username: {Username}", username);
                return CredentialValidationResult.Invalid();
            }

            if (user.IsLocked(now))
            {
                _logger.LogWarning("Credenciales inválidas: cuenta bloqueada hasta {LockedUntil}. UserId: {UserId}",
                    user.PasswordLock.LockedUntil, user.Id);
                return CredentialValidationResult.Blocked(user);
            }

            if (!_passwordHashingService.VerifyPassword(password, user.Password))
            {
                user.RegisterFailedPasswordAttempt(_loginOptions.MaxAttempts,
                    TimeSpan.FromMinutes(_loginOptions.LockDurationMinutes), now);
                await _userRepository.UpdateAsync(user, ct);

                _logger.LogWarning("Credenciales inválidas: contraseña incorrecta. UserId: {UserId}, Intentos: {Attempts}",
                    user.Id, user.PasswordLock.FailedAttempts);

                return user.IsLocked(now)
                    ? CredentialValidationResult.Blocked(user)
                    : CredentialValidationResult.Invalid();
            }

            user.RegisterSuccessfulLogin(now);
            await _userRepository.UpdateAsync(user, ct);

            return CredentialValidationResult.Success(user);
        }
    }
}
