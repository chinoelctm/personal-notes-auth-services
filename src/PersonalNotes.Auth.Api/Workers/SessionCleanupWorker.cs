using MediatR;
using PersonalNotes.Auth.Application.Commands.Maintenance;
using PersonalNotes.Auth.Application.Helpers;
using PersonalNotes.Auth.Domain.Options;

namespace PersonalNotes.Auth.Api.Workers
{
    /// <summary>Borra cada día (SessionSettings:SessionCleanupTime, hora de Chile) los refresh tokens vencidos, usados o revocados.</summary>
    public sealed class SessionCleanupWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SessionCleanupWorker> _logger;
        private readonly ISessionOptions _sessionOptions;
        private readonly TimeHelper _timeHelper;
        private DateTime _lastExecutionDate = DateTime.MinValue;

        public SessionCleanupWorker(IServiceScopeFactory scopeFactory, ILogger<SessionCleanupWorker> logger,
            ISessionOptions sessionOptions, TimeHelper timeHelper)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _sessionOptions = sessionOptions;
            _timeHelper = timeHelper;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            _logger.LogInformation("SessionCleanupWorker iniciado. Ejecución diaria a las {Time}.",
                _sessionOptions.SessionCleanupTime);

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var delay = GetDelayUntilNextExecution();
                    _logger.LogInformation("[SessionCleanup] Próxima ejecución en {Hours}h {Minutes}m",
                        (int)delay.TotalHours, delay.Minutes);

                    await Task.Delay(delay, ct);

                    var nowLocal = _timeHelper.GetLocalNow();
                    if (nowLocal.Date == _lastExecutionDate.Date)
                        continue;

                    await CleanAsync(ct);
                    _lastExecutionDate = nowLocal;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[SessionCleanup] Error durante la limpieza. Reintentando en 1 hora.");

                    try
                    {
                        await Task.Delay(TimeSpan.FromHours(1), ct);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
        }

        private TimeSpan GetDelayUntilNextExecution()
        {
            var nowLocal = _timeHelper.GetLocalNow();
            var nextRunLocal = nowLocal.Date.Add(TimeSpan.Parse(_sessionOptions.SessionCleanupTime));

            if (nowLocal >= nextRunLocal)
                nextRunLocal = nextRunLocal.AddDays(1);

            return nextRunLocal - nowLocal;
        }

        private async Task CleanAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var deleted = await mediator.Send(new CleanExpiredSessionsCommand(), ct);
            _logger.LogInformation("[SessionCleanup] Limpieza completada: {Count} sesión(es) eliminada(s).", deleted);
        }
    }
}
