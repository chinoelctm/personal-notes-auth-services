using MediatR;

namespace PersonalNotes.Auth.Application.Commands.Auth.Logout
{
    public record LogoutCommand(string? RefreshToken) : IRequest;
}
