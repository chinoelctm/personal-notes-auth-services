using MongoDB.Bson.Serialization.Attributes;

namespace PersonalNotes.Auth.Domain.ValueObjects
{
    /// <summary>
    /// Bloqueo por contraseña incorrecta. A diferencia de Iam.Service (bloqueo permanente hasta que un
    /// administrador lo libere), aquí el bloqueo dura un tiempo: no hay administrador que desbloquee.
    /// </summary>
    public class PasswordLockState
    {
        [BsonElement("failedAttempts")] public int FailedAttempts { get; private set; }

        [BsonElement("lockedUntil")]
        [BsonIgnoreIfNull]
        public DateTime? LockedUntil { get; private set; }

        [BsonElement("lastFailedOn")]
        [BsonIgnoreIfNull]
        public DateTime? LastFailedOn { get; private set; }

        public PasswordLockState() { }

        public bool IsLocked(DateTime now) => LockedUntil.HasValue && now < LockedUntil.Value;

        public void RegisterFailedAttempt(int maxAttempts, TimeSpan lockDuration, DateTime now)
        {
            if (IsLocked(now)) return;

            // El bloqueo anterior ya venció: se parte de cero
            if (LockedUntil.HasValue)
            {
                FailedAttempts = 0;
                LockedUntil = null;
            }

            FailedAttempts++;
            LastFailedOn = now;

            if (FailedAttempts >= maxAttempts)
                LockedUntil = now.Add(lockDuration);
        }

        public void Reset()
        {
            FailedAttempts = 0;
            LastFailedOn = null;
            LockedUntil = null;
        }
    }
}
