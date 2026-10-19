using MediatR;
using PersonalNotes.Auth.Application.Services.Tokens;

namespace PersonalNotes.Auth.Application.Commands.RefreshToken
{
    public record RefreshTokenCommand(string RefreshToken) : IRequest<TokenIssueResult>;
}
