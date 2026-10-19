using MediatR;
using Microsoft.Extensions.Logging;
using PersonalNotes.Auth.Application.Helpers;
using PersonalNotes.Auth.Domain.Entities;
using PersonalNotes.Auth.Domain.Repositories;
using PersonalNotes.Auth.Domain.Services;

namespace PersonalNotes.Auth.Application.Commands.Users.EnsureSeed
{
    /// <summary>
    /// Crea el usuario inicial si no existe y, si ya existe, le deja la contraseña de SeedUser:PasswordHash. No
    /// hay endpoint para cambiar la contraseña: la configuración es la única fuente de verdad, así que cambiar
    /// el hash en Render y reiniciar es la forma de cambiarla (o de recuperarla si un deploy guardó otra).
    /// </summary>
    public class EnsureSeedUserHandler : IRequestHandler<EnsureSeedUserCommand, bool>
    {
        private readonly ILogger<EnsureSeedUserHandler> _logger;
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHashingService _passwordHashingService;
        private readonly TimeHelper _timeHelper;

        public EnsureSeedUserHandler(ILogger<EnsureSeedUserHandler> logger, IUserRepository userRepository,
            IPasswordHashingService passwordHashingService, TimeHelper timeHelper)
        {
            _logger = logger;
            _userRepository = userRepository;
            _passwordHashingService = passwordHashingService;
            _timeHelper = timeHelper;
        }

        public async Task<bool> Handle(EnsureSeedUserCommand command, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(command.Username) || !_passwordHashingService.IsPasswordHash(command.PasswordHash))
            {
                _logger.LogWarning("SeedUser sin Username o sin un hash BCrypt válido en PasswordHash: no se crea el usuario inicial.");
                return false;
            }

            var now = _timeHelper.GetUtcNow();
            var existing = await _userRepository.GetByUsernameAsync(command.Username, ct);
            if (existing is not null)
            {
                if (existing.Password == command.PasswordHash)
                    return false;

                // Limpia también los intentos fallidos y el bloqueo
                existing.UpdatePassword(command.PasswordHash, now);
                await _userRepository.UpdateAsync(existing, ct);
                _logger.LogWarning("La contraseña del usuario inicial {Username} se actualizó desde SeedUser:PasswordHash.",
                    existing.Username);
                return true;
            }

            var user = User.Create(command.Username, command.Name, command.PasswordHash, now);
            await _userRepository.AddAsync(user, ct);

            _logger.LogInformation("Usuario inicial {Username} creado.", user.Username);
            return true;
        }
    }
}
