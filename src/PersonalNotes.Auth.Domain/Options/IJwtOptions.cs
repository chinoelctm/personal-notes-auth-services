namespace PersonalNotes.Auth.Domain.Options
{
    public interface IJwtOptions
    {
        string PrivateKeyPem { get; }
        string Issuer { get; }
        string Audience { get; }
        int AccessTokenMinutes { get; }
        int RefreshTokenDays { get; }
    }
}
