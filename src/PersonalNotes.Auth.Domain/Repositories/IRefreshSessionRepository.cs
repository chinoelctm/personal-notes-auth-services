using PersonalNotes.Auth.Domain.Entities;

namespace PersonalNotes.Auth.Domain.Repositories
{
    public interface IRefreshSessionRepository
    {
        Task<Guid> AddAsync(RefreshSession refreshSession, CancellationToken ct = default);
        Task<RefreshSession?> GetAsync(string tokenHash, CancellationToken ct = default);
        Task UpdateAsync(RefreshSession refreshSession, CancellationToken ct = default);
        Task DeleteRangeAsync(List<Guid> ids, CancellationToken ct = default);
        Task<List<RefreshSession>> GetExpiredAsync(DateTime now, CancellationToken ct = default);
    }
}
