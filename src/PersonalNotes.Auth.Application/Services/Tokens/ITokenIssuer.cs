using PersonalNotes.Auth.Domain.Entities;

namespace PersonalNotes.Auth.Application.Services.Tokens
{
    public interface ITokenIssuer
    {
        Task<TokenIssueResult> IssueAsync(User user, CancellationToken ct);
    }
}
