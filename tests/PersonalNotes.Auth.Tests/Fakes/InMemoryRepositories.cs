using PersonalNotes.Auth.Domain.Entities;
using PersonalNotes.Auth.Domain.Repositories;

namespace PersonalNotes.Auth.Tests.Fakes
{
    public class InMemoryUserRepository : IUserRepository
    {
        public List<User> Users { get; } = [];

        public Task<Guid> AddAsync(User user, CancellationToken ct = default)
        {
            Users.Add(user);
            return Task.FromResult(user.Id);
        }

        public Task UpdateAsync(User user, CancellationToken ct = default) => Task.CompletedTask;

        public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(Users.FirstOrDefault(u => u.Id == id));

        public Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default)
            => Task.FromResult(Users.FirstOrDefault(u => u.NormalizedUsername == User.Normalize(username)));
    }

    public class InMemoryRefreshSessionRepository : IRefreshSessionRepository
    {
        public List<RefreshSession> Sessions { get; } = [];

        public Task<Guid> AddAsync(RefreshSession refreshSession, CancellationToken ct = default)
        {
            Sessions.Add(refreshSession);
            return Task.FromResult(refreshSession.Id);
        }

        public Task<RefreshSession?> GetAsync(string tokenHash, CancellationToken ct = default)
            => Task.FromResult(Sessions.FirstOrDefault(s => s.Token == tokenHash));

        public Task UpdateAsync(RefreshSession refreshSession, CancellationToken ct = default) => Task.CompletedTask;

        public Task DeleteRangeAsync(List<Guid> ids, CancellationToken ct = default)
        {
            Sessions.RemoveAll(s => ids.Contains(s.Id));
            return Task.CompletedTask;
        }

        public Task<List<RefreshSession>> GetExpiredAsync(DateTime now, CancellationToken ct = default)
            => Task.FromResult(Sessions.Where(s => s.IsExpired(now)).ToList());
    }
}
