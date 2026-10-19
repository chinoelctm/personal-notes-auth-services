namespace PersonalNotes.Auth.Domain.Services
{
    public interface IPasswordHashingService
    {
        string HashPassword(string password);
        bool VerifyPassword(string plainPassword, string bcryptHash);
        string HashToken(string plainToken);
    }
}
