using PersonalNotes.Auth.Domain.Entities;

namespace PersonalNotes.Auth.Application.Services.Auth
{
    public sealed class CredentialValidationResult
    {
        public User? User { get; }
        public bool IsValid { get; }
        public bool IsBlocked { get; }
        public DateTime? LockedUntil { get; }

        private CredentialValidationResult(User? user, bool isValid, bool isBlocked, DateTime? lockedUntil)
        {
            User = user;
            IsValid = isValid;
            IsBlocked = isBlocked;
            LockedUntil = lockedUntil;
        }

        public static CredentialValidationResult Success(User user)
            => new(user, isValid: true, isBlocked: false, lockedUntil: null);

        public static CredentialValidationResult Blocked(User user)
            => new(user, isValid: false, isBlocked: true, lockedUntil: user.PasswordLock.LockedUntil);

        public static CredentialValidationResult Invalid()
            => new(null, isValid: false, isBlocked: false, lockedUntil: null);
    }
}
