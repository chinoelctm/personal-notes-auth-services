using MediatR;
using Microsoft.Extensions.Logging;
using PersonalNotes.Auth.Application.Helpers;
using PersonalNotes.Auth.Domain.Entities;
using PersonalNotes.Auth.Domain.Repositories;

namespace PersonalNotes.Auth.Application.Commands.Users.EnsureSeed
{
    /// <summary>
    /// No modifica un usuario que ya existe: cambiar el hash en la configuración no reemplaza la contraseña
    /// guardada (así un reinicio no deshace un cambio de contraseña).
    /// </summary>
    public class EnsureSeedUserHandler : IRequestHandler<EnsureSeedUserCommand, bool>
    {
        private readonly ILogger<EnsureSeedUserHandler> _logger;
        private readonly IUserRepository _userRepository;
        private readonly TimeHelper _timeHelper;

        public EnsureSeedUserHandler(ILogger<EnsureSeedUserHandler> logger, IUserRepository userRepository, TimeHelper timeHelper)
        {
            _logger = logger;
            _userRepository = userRepository;
            _timeHelper = timeHelper;
        }

        public async Task<bool> Handle(EnsureSeedUserCommand command, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(command.Username) || string.IsNullOrWhiteSpace(command.PasswordHash))
            {
                _logger.LogWarning("SeedUser sin Username o PasswordHash: no se crea el usuario inicial.");
                return false;
            }

            if (await _userRepository.GetByUsernameAsync(command.Username, ct) is not null)
                return false;

            var user = User.Create(command.Username, command.Name, command.PasswordHash, _timeHelper.GetUtcNow());
            await _userRepository.AddAsync(user, ct);

            _logger.LogInformation("Usuario inicial {Username} creado.", user.Username);
            return true;
        }
    }
}
