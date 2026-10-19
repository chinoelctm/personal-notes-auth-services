using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using PersonalNotes.Auth.Domain.Services;

namespace PersonalNotes.Auth.Infrastructure.Services
{
    public class PasswordHashingService : IPasswordHashingService
    {
        public string HashPassword(string password) =>
            BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

        public bool VerifyPassword(string plainPassword, string bcryptHash)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(plainPassword, bcryptHash);
            }
            catch (BCrypt.Net.SaltParseException)
            {
                // Hash mal configurado: se trata como contraseña incorrecta en vez de un 500
                return false;
            }
        }

        public bool IsPasswordHash(string? value) => IsBcryptHash(value);

        public static bool IsBcryptHash(string? value) =>
            value is not null && Regex.IsMatch(value, @"^\$2[aby]\$\d{2}\$[./A-Za-z0-9]{53}$");

        // Los refresh tokens son aleatorios de 256 bits: basta SHA256 (BCrypt sería lento y no buscable)
        public string HashToken(string plainToken)
        {
            var bytes = Encoding.UTF8.GetBytes(plainToken.Trim());
            return Convert.ToBase64String(SHA256.HashData(bytes));
        }
    }
}
