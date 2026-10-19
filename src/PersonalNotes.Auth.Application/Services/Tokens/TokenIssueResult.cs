namespace PersonalNotes.Auth.Application.Services.Tokens
{
    public sealed record TokenIssueResult(
        string AccessToken,
        string RefreshToken,
        int AccessTokenExpiresIn,
        int RefreshTokenExpiresIn
    );
}
