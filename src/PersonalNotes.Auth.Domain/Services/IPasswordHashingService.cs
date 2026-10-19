namespace PersonalNotes.Auth.Domain.Services
{
    public interface IPasswordHashingService
    {
        string HashPassword(string password);
        bool VerifyPassword(string plainPassword, string bcryptHash);
        string HashToken(string plainToken);

        /// <summary>true si el texto tiene formato de hash BCrypt ($2a$/$2b$/$2y$, costo y 53 caracteres).</summary>
        bool IsPasswordHash(string? value);
    }
}
