using MongoDB.Bson.Serialization.Attributes;

namespace PersonalNotes.Auth.Domain.Entities
{
    /// <summary>
    /// Refresh token opaco. Solo se guarda su hash (SHA256): con una copia de la base no se pueden renovar
    /// sesiones. Cada uso lo marca como usado y emite uno nuevo (rotación).
    /// </summary>
    [BsonIgnoreExtraElements]
    public class RefreshSession
    {
        [BsonId] public Guid Id { get; private set; }

        [BsonElement("userId")] public Guid UserId { get; private set; }

        [BsonElement("token")] public string Token { get; private set; } = null!;

        [BsonElement("createdAt")] public DateTime CreatedAt { get; private set; }

        [BsonElement("expiresAt")] public DateTime ExpiresAt { get; private set; }

        [BsonElement("isUsed")] public bool IsUsed { get; private set; }

        [BsonElement("usedAt")]
        [BsonIgnoreIfNull]
        public DateTime? UsedAt { get; private set; }

        [BsonElement("revokedAt")]
        [BsonIgnoreIfNull]
        public DateTime? RevokedAt { get; private set; }

        private RefreshSession()
        {
        }

        public static RefreshSession Create(Guid userId, string tokenHash, DateTime now, int expirationDays = 7)
        {
            return new RefreshSession
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Token = tokenHash,
                CreatedAt = now,
                ExpiresAt = now.AddDays(expirationDays),
                IsUsed = false
            };
        }

        public bool IsExpired(DateTime now) => now > ExpiresAt;

        public bool IsRevoked() => RevokedAt.HasValue;

        public void MarkAsUsed(DateTime now)
        {
            IsUsed = true;
            UsedAt = now;
        }

        public void Revoke(DateTime now)
        {
            RevokedAt = now;
        }
    }
}
