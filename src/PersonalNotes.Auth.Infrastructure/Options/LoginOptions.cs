using System.ComponentModel.DataAnnotations;
using PersonalNotes.Auth.Domain.Options;

namespace PersonalNotes.Auth.Infrastructure.Options
{
    public class LoginOptions : ILoginOptions
    {
        public const string SectionName = "LoginSettings";

        [Range(1, 100)] public int MaxAttempts { get; set; } = 5;
        [Range(1, 1440)] public int LockDurationMinutes { get; set; } = 15;
    }
}
