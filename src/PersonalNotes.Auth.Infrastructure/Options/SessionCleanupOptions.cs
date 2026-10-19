using System.ComponentModel.DataAnnotations;
using PersonalNotes.Auth.Domain.Options;

namespace PersonalNotes.Auth.Infrastructure.Options
{
    public class SessionCleanupOptions : ISessionOptions
    {
        public const string SectionName = "SessionSettings";

        [RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$", ErrorMessage = "SessionCleanupTime debe tener el formato HH:mm.")]
        public string SessionCleanupTime { get; set; } = "23:00";
    }
}
