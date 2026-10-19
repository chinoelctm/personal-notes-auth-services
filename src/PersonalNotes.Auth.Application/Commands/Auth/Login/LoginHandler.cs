using MediatR;
using Microsoft.Extensions.Logging;
using PersonalNotes.Auth.Application.Queries.Users.GetCurrent;
using PersonalNotes.Auth.Application.Services.Auth;
using PersonalNotes.Auth.Application.Services.Tokens;
using PersonalNotes.Auth.Domain.Exceptions;

namespace PersonalNotes.Auth.Application.Commands.Auth.Login
{
    /// <summary>Iam.Service hace login en dos pasos (credenciales + OTP); aquí las credenciales emiten los tokens directo.</summary>
    public class LoginHandler : IRequestHandler<LoginCommand, LoginResponse>
    {
        private readonly ILogger<LoginHandler> _logger;
        private readonly ICredentialValidator _credentialValidator;
        private readonly ITokenIssuer _tokenIssuer;

        public LoginHandler(ILogger<LoginHandler> logger, ICredentialValidator credentialValidator, ITokenIssuer tokenIssuer)
        {
            _logger = logger;
            _credentialValidator = credentialValidator;
            _tokenIssuer = tokenIssuer;
        }

        public async Task<LoginResponse> Handle(LoginCommand command, CancellationToken ct)
        {
            var validation = await _credentialValidator.ValidateAsync(command.Username, command.Password, ct);

            if (!validation.IsValid)
            {
                if (validation.IsBlocked)
                {
                    throw new LockedException(
                        "La cuenta está bloqueada por demasiados intentos fallidos. Intenta de nuevo en unos minutos.");
                }
                throw new UnauthorizedException("Usuario y/o contraseña inválidos.");
            }

            var user = validation.User!;
            var tokens = await _tokenIssuer.IssueAsync(user, ct);

            _logger.LogInformation("Login exitoso. UserId: {UserId}", user.Id);

            return new LoginResponse(tokens, CurrentUserResponse.From(user));
        }
    }
}
