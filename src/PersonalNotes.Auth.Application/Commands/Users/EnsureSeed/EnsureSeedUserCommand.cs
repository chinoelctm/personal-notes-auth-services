using MediatR;

namespace PersonalNotes.Auth.Application.Commands.Users.EnsureSeed
{
    /// <summary>Crea el usuario inicial si no existe. Devuelve true si lo creó.</summary>
    public record EnsureSeedUserCommand(
        string Username,
        string Name,
        string PasswordHash
    ) : IRequest<bool>;
}
