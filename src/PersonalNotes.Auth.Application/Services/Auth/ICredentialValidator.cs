namespace PersonalNotes.Auth.Application.Services.Auth
{
    public interface ICredentialValidator
    {
        Task<CredentialValidationResult> ValidateAsync(string username, string password, CancellationToken ct);
    }
}
