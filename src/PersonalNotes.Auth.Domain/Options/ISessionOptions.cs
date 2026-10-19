namespace PersonalNotes.Auth.Domain.Options
{
    public interface ISessionOptions
    {
        /// <summary>Hora local (HH:mm, America/Santiago) de la limpieza diaria de refresh tokens vencidos.</summary>
        string SessionCleanupTime { get; }
    }
}
