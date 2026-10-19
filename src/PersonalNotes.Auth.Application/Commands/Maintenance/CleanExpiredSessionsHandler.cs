using MediatR;
using Microsoft.Extensions.Logging;
using PersonalNotes.Auth.Application.Helpers;
using PersonalNotes.Auth.Domain.Repositories;

namespace PersonalNotes.Auth.Application.Commands.Maintenance
{
    public record CleanExpiredSessionsCommand : IRequest<int>;

    public class CleanExpiredSessionsHandler : IRequestHandler<CleanExpiredSessionsCommand, int>
    {
        private readonly IRefreshSessionRepository _refreshSessions;
        private readonly ILogger<CleanExpiredSessionsHandler> _logger;
        private readonly TimeHelper _timeHelper;

        public CleanExpiredSessionsHandler(IRefreshSessionRepository refreshSessions,
            ILogger<CleanExpiredSessionsHandler> logger, TimeHelper timeHelper)
        {
            _refreshSessions = refreshSessions;
            _logger = logger;
            _timeHelper = timeHelper;
        }

        public async Task<int> Handle(CleanExpiredSessionsCommand command, CancellationToken ct)
        {
            var expired = await _refreshSessions.GetExpiredAsync(_timeHelper.GetUtcNow(), ct);
            if (expired.Count == 0)
                return 0;

            await _refreshSessions.DeleteRangeAsync(expired.Select(x => x.Id).ToList(), ct);
            _logger.LogInformation("Refresh tokens vencidos, usados o revocados eliminados: {Count}", expired.Count);

            return expired.Count;
        }
    }
}
