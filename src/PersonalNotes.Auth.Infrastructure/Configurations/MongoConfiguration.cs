using System.ComponentModel.DataAnnotations;

namespace PersonalNotes.Auth.Infrastructure.Configurations
{
    public class MongoConfiguration
    {
        public const string SectionName = "ConnectionStrings";

        [Required] public string MongoUri { get; init; } = null!;
        [Required] public string MongoDb  { get; init; } = null!;

        public int MaxConnectionPool  { get; init; } = 100;
        public int MinConnectionPool  { get; init; } = 5;
        public int MaxIdleMinutes     { get; init; } = 5;
        public int MaxLifetimeMinutes { get; init; } = 30;
        public int TimeoutMs          { get; init; } = 30000;
    }
}
