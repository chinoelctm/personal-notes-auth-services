namespace PersonalNotes.Auth.Domain.Options
{
    public interface ILoginOptions
    {
        int MaxAttempts { get; }
        int LockDurationMinutes { get; }
    }
}
