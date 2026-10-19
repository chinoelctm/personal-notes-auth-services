using PersonalNotes.Auth.Application.Queries.Users.GetCurrent;
using PersonalNotes.Auth.Application.Services.Tokens;

namespace PersonalNotes.Auth.Application.Commands.Auth.Login
{
    /// <summary>Los tokens los convierte en cookies la capa HTTP; al cliente solo le llega el usuario.</summary>
    public record LoginResponse(
        TokenIssueResult Tokens,
        CurrentUserResponse User
    );
}
