using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using PersonalNotes.Auth.Api.Workers;
using PersonalNotes.Auth.Application.Helpers;
using PersonalNotes.Auth.Domain.Options;
using PersonalNotes.Auth.Domain.Repositories;
using PersonalNotes.Auth.Domain.Services;
using PersonalNotes.Auth.Infrastructure.Configurations;
using PersonalNotes.Auth.Infrastructure.Context;
using PersonalNotes.Auth.Infrastructure.Options;
using PersonalNotes.Auth.Infrastructure.Persistence;
using PersonalNotes.Auth.Infrastructure.Services;

namespace PersonalNotes.Auth.Api.Extensions
{
    public static class InfrastructureExtensions
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddMongoDb(configuration)
                .AddTimezone()
                .AddRepositoriesAndServices()
                .AddOptions(configuration);

            return services;
        }

        private static IServiceCollection AddMongoDb(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<MongoConfiguration>()
                .Bind(configuration.GetSection(MongoConfiguration.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            BsonSerializer.TryRegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

            services.AddSingleton<IMongoClient>(sp =>
            {
                var opts     = sp.GetRequiredService<IOptions<MongoConfiguration>>().Value;
                var settings = MongoClientSettings.FromConnectionString(opts.MongoUri);

                settings.MaxConnectionPoolSize  = opts.MaxConnectionPool;
                settings.MinConnectionPoolSize  = opts.MinConnectionPool;
                settings.MaxConnectionIdleTime  = TimeSpan.FromMinutes(opts.MaxIdleMinutes);
                settings.MaxConnectionLifeTime  = TimeSpan.FromMinutes(opts.MaxLifetimeMinutes);
                settings.RetryReads             = true;
                settings.RetryWrites            = true;
                settings.ConnectTimeout         = TimeSpan.FromMilliseconds(opts.TimeoutMs);
                settings.ServerSelectionTimeout = TimeSpan.FromMilliseconds(opts.TimeoutMs);
                settings.SocketTimeout          = TimeSpan.FromMilliseconds(opts.TimeoutMs);
                settings.WaitQueueTimeout       = TimeSpan.FromMilliseconds(opts.TimeoutMs);

                return new MongoClient(settings);
            });

            services.AddSingleton(sp =>
            {
                var client = sp.GetRequiredService<IMongoClient>();
                var opts   = sp.GetRequiredService<IOptions<MongoConfiguration>>().Value;
                return new AuthDbContext(client, opts.MongoDb);
            });

            return services;
        }

        // Las fechas se guardan en UTC; la zona de Chile solo agenda la limpieza diaria
        private static IServiceCollection AddTimezone(this IServiceCollection services)
        {
            var timezoneId = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "Pacific SA Standard Time"
                : "America/Santiago";

            services.AddSingleton(TimeZoneInfo.FindSystemTimeZoneById(timezoneId));
            services.AddSingleton<TimeHelper>();

            return services;
        }

        private static IServiceCollection AddRepositoriesAndServices(this IServiceCollection services)
        {
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRefreshSessionRepository, RefreshSessionRepository>();
            services.AddSingleton<IPasswordHashingService, PasswordHashingService>();
            services.AddSingleton<IJwtTokenService, JwtTokenService>();
            services.AddSingleton<IDatabaseHealth, MongoDatabaseHealth>();

            // Índices y usuario inicial en segundo plano: la API arranca aunque MongoDB todavía no responda
            services.AddHostedService<DatabaseInitializer>();
            services.AddHostedService<SessionCleanupWorker>();

            return services;
        }

        private static IServiceCollection AddOptions(this IServiceCollection services, IConfiguration configuration)
        {
            // Una clave inválida detiene el arranque con un mensaje claro, en vez de un 500 en cada request
            services.AddOptions<JwtOptions>()
                .Bind(configuration.GetSection(JwtOptions.SectionName))
                .ValidateDataAnnotations()
                .Validate(o => string.IsNullOrWhiteSpace(o.PrivateKeyPem) || PemKeys.TryImportRsa(o.PrivateKeyPem, requirePrivateKey: true, out _),
                    "JwtSettings:PrivateKeyPem no es una clave RSA privada válida en PEM. Copia el archivo jwt-private.pem completo, " +
                    "incluidas las líneas -----BEGIN PRIVATE KEY----- y -----END PRIVATE KEY-----.")
                .ValidateOnStart();

            services.AddSingleton<IJwtOptions>(sp =>
                sp.GetRequiredService<IOptions<JwtOptions>>().Value);

            services.AddOptions<LoginOptions>()
                .Bind(configuration.GetSection(LoginOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddSingleton<ILoginOptions>(sp =>
                sp.GetRequiredService<IOptions<LoginOptions>>().Value);

            services.AddOptions<SessionCleanupOptions>()
                .Bind(configuration.GetSection(SessionCleanupOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddSingleton<ISessionOptions>(sp =>
                sp.GetRequiredService<IOptions<SessionCleanupOptions>>().Value);

            // Vacío se permite (no se crea el usuario), pero un valor que no es BCrypt detiene el arranque
            services.AddOptions<SeedUserOptions>()
                .Bind(configuration.GetSection(SeedUserOptions.SectionName))
                .Validate(o => string.IsNullOrWhiteSpace(o.PasswordHash) || PasswordHashingService.IsBcryptHash(o.PasswordHash.Trim()),
                    "SeedUser:PasswordHash no es un hash BCrypt ($2a$12$ seguido de 53 caracteres). Copia el contenido de " +
                    "seed-password-hash.txt, no la contraseña ni la clave privada.")
                .ValidateOnStart();

            return services;
        }
    }
}
