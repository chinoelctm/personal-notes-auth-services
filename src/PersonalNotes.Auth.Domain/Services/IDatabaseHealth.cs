namespace PersonalNotes.Auth.Domain.Services
{
    public interface IDatabaseHealth
    {
        Task<bool> PingAsync(CancellationToken ct = default);
    }
}
