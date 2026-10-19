using MediatR;

namespace PersonalNotes.Auth.Application.Commands.Auth.Login
{
    public record LoginCommand(
        string Username,
        string Password
    ) : IRequest<LoginResponse>;
}
