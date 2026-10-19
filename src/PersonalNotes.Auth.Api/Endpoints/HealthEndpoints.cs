using MediatR;
using PersonalNotes.Auth.Application.Queries.Health;

namespace PersonalNotes.Auth.Api.Endpoints
{
    public static class HealthEndpoints
    {
        public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/v1/health")
                .WithTags("Health")
                .WithOpenApi();

            group.MapGet("/", GetAsync)
                .WithSummary("Estado de la API y de la conexión a MongoDB. 503 si la base no responde.");

            return app;
        }

        private static async Task<IResult> GetAsync(IMediator mediator, CancellationToken ct)
        {
            var result = await mediator.Send(new GetHealthQuery(), ct);
            return result.IsHealthy
                ? Results.Ok(result)
                : Results.Json(result, statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }
}
