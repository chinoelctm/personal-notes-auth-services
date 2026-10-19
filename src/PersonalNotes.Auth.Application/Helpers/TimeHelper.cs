namespace PersonalNotes.Auth.Application.Helpers
{
    public class TimeHelper
    {
        private readonly TimeZoneInfo _timezone;

        public TimeHelper(TimeZoneInfo timezone)
        {
            _timezone = timezone;
        }

        /// <summary>
        /// Instante actual en UTC: vencimientos de sesiones y fechas de auditoría. Mongo guarda las fechas en UTC
        /// y trata un DateTime sin Kind como hora local del servidor, que en un contenedor no es la de Chile.
        /// </summary>
        public virtual DateTime GetUtcNow() => DateTime.UtcNow;

        /// <summary>Hora local (America/Santiago), solo para agendar la limpieza diaria.</summary>
        public virtual DateTime GetLocalNow() => TimeZoneInfo.ConvertTimeFromUtc(GetUtcNow(), _timezone);
    }
}
