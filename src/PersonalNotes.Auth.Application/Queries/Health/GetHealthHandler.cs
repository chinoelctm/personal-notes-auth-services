using MediatR;
using PersonalNotes.Auth.Domain.Services;

namespace PersonalNotes.Auth.Application.Queries.Health
{
    public class GetHealthHandler : IRequestHandler<GetHealthQuery, GetHealthResponse>
    {
        private readonly IDatabaseHealth _databaseHealth;

        public GetHealthHandler(IDatabaseHealth databaseHealth)
        {
            _databaseHealth = databaseHealth;
        }

        public async Task<GetHealthResponse> Handle(GetHealthQuery query, CancellationToken ct)
        {
            return await _databaseHealth.PingAsync(ct)
                ? new GetHealthResponse("Healthy", "Up")
                : new GetHealthResponse("Degraded", "Down");
        }
    }
}
