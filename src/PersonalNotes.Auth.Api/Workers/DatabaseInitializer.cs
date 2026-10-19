using MediatR;
using Microsoft.Extensions.Options;
using PersonalNotes.Auth.Application.Commands.Users.EnsureSeed;
using PersonalNotes.Auth.Infrastructure.Context;
using PersonalNotes.Auth.Infrastructure.Options;

namespace PersonalNotes.Auth.Api.Workers
{
    /// <summary>
    /// Crea los índices de MongoDB y el usuario inicial en segundo plano. Si la base no responde al arrancar
    /// (Atlas bloquea la IP o aún no está disponible), la API sigue levantada y se reintenta con espera creciente.
    /// </summary>
    public class DatabaseInitializer : BackgroundService
    {
        private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan MaxDelay = TimeSpan.FromMinutes(5);

        private readonly AuthDbContext _context;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly SeedUserOptions _seedUser;
        private readonly ILogger<DatabaseInitializer> _logger;

        public DatabaseInitializer(AuthDbContext context, IServiceScopeFactory scopeFactory,
            IOptions<SeedUserOptions> seedUser, ILogger<DatabaseInitializer> logger)
        {
            _context = context;
            _scopeFactory = scopeFactory;
            _seedUser = seedUser.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // No retrasar el arranque del servidor HTTP
            await Task.Yield();

            var delay = InitialDelay;
            for (var attempt = 1; !stoppingToken.IsCancellationRequested; attempt++)
            {
                try
                {
                    await _context.EnsureIndexesAsync(stoppingToken);
                    _logger.LogInformation("Índices de MongoDB verificados (intento {Attempt}).", attempt);

                    using var scope = _scopeFactory.CreateScope();
                    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                    await mediator.Send(new EnsureSeedUserCommand(_seedUser.Username, _seedUser.Name, _seedUser.PasswordHash),
                        stoppingToken);
                    return;
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "No se pudo inicializar MongoDB (intento {Attempt}). Reintento en {Seconds}s.",
                        attempt, delay.TotalSeconds);
                }

                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                delay = TimeSpan.FromTicks(Math.Min(delay.Ticks * 2, MaxDelay.Ticks));
            }
        }
    }
}
