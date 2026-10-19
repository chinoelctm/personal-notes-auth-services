using MongoDB.Bson.Serialization.Attributes;
using PersonalNotes.Auth.Domain.Exceptions;
using PersonalNotes.Auth.Domain.ValueObjects;

namespace PersonalNotes.Auth.Domain.Entities
{
    [BsonIgnoreExtraElements]
    public class User
    {
        public const int MaxUsernameLength = 50;
        public const int MaxNameLength = 100;

        [BsonId] public Guid Id { get; private set; }

        [BsonElement("username")] public string Username { get; private set; } = null!;

        /// <summary>Username en minúsculas: el login no distingue mayúsculas y el índice único lo usa.</summary>
        [BsonElement("normalizedUsername")] public string NormalizedUsername { get; private set; } = null!;

        [BsonElement("name")] public string Name { get; private set; } = null!;

        /// <summary>Hash BCrypt de la contraseña; nunca la contraseña en texto plano.</summary>
        [BsonElement("password")] public string Password { get; private set; } = null!;

        [BsonElement("enabled")] public bool Enabled { get; private set; }

        [BsonElement("passwordLock")] public PasswordLockState PasswordLock { get; private set; } = new();

        [BsonElement("createdOn")] public DateTime CreatedOn { get; private set; }

        [BsonElement("updatedOn")]
        [BsonIgnoreIfNull]
        public DateTime? UpdatedOn { get; private set; }

        [BsonElement("lastLoginOn")]
        [BsonIgnoreIfNull]
        public DateTime? LastLoginOn { get; private set; }

        private User()
        {
        }

        public static User Create(string username, string? name, string passwordHash, DateTime now)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new DomainException("El nombre de usuario es obligatorio.");
            if (username.Trim().Length > MaxUsernameLength)
                throw new DomainException($"El nombre de usuario no puede superar {MaxUsernameLength} caracteres.");
            if (string.IsNullOrWhiteSpace(passwordHash))
                throw new DomainException("La contraseña es obligatoria.");

            var cleanUsername = username.Trim();
            var cleanName = string.IsNullOrWhiteSpace(name) ? cleanUsername : name.Trim();
            if (cleanName.Length > MaxNameLength)
                throw new DomainException($"El nombre no puede superar {MaxNameLength} caracteres.");

            return new User
            {
                Id = Guid.NewGuid(),
                Username = cleanUsername,
                NormalizedUsername = Normalize(cleanUsername),
                Name = cleanName,
                Password = passwordHash,
                Enabled = true,
                PasswordLock = new PasswordLockState(),
                CreatedOn = now
            };
        }

        public static string Normalize(string username) => username.Trim().ToLowerInvariant();

        public bool IsActive => Enabled;

        public bool IsLocked(DateTime now) => PasswordLock.IsLocked(now);

        public void UpdatePassword(string passwordHash, DateTime now)
        {
            if (string.IsNullOrWhiteSpace(passwordHash)) return;
            Password = passwordHash;
            PasswordLock.Reset();
            UpdatedOn = now;
        }

        public void RegisterFailedPasswordAttempt(int maxAttempts, TimeSpan lockDuration, DateTime now)
        {
            PasswordLock.RegisterFailedAttempt(maxAttempts, lockDuration, now);
            UpdatedOn = now;
        }

        public void RegisterSuccessfulLogin(DateTime now)
        {
            PasswordLock.Reset();
            LastLoginOn = now;
        }

        public void Disable(DateTime now)
        {
            if (!Enabled) return;
            Enabled = false;
            UpdatedOn = now;
        }
    }
}
