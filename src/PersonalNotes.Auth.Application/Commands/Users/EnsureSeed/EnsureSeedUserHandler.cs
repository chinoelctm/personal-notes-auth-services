using MediatR;
using Microsoft.Extensions.Logging;
using PersonalNotes.Auth.Application.Helpers;
using PersonalNotes.Auth.Domain.Entities;
using PersonalNotes.Auth.Domain.Repositories;
using PersonalNotes.Auth.Domain.Services;

namespace PersonalNotes.Auth.Application.Commands.Users.EnsureSeed
{
    /// <summary>
    /// Crea el usuario inicial si no existe. A un usuario existente no le cambia una contraseña válida (así un
    /// reinicio no deshace un cambio de contraseña), pero sí repara una guardada que no es un hash BCrypt: pasa
    /// si un deploy arrancó con un valor equivocado en SeedUser__PasswordHash y con él no se podía entrar nunca.
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
                if (_passwordHashingService.IsPasswordHash(existing.Password))
                    return false;

                existing.UpdatePassword(command.PasswordHash, now);
                await _userRepository.UpdateAsync(existing, ct);
                _logger.LogWarning("El usuario inicial {Username} tenía una contraseña guardada inválida; se reemplazó por SeedUser:PasswordHash.",
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
