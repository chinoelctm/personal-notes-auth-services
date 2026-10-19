using System.Text.Json.Serialization;
using MediatR;

namespace PersonalNotes.Auth.Application.Queries.Health
{
    public record GetHealthQuery : IRequest<GetHealthResponse>;

    /// <param name="Status">Healthy si todo responde; Degraded si la API está arriba pero la base no.</param>
    /// <param name="Database">Up o Down.</param>
    public record GetHealthResponse(string Status, string Database)
    {
        [JsonIgnore]
        public bool IsHealthy => Database == "Up";
    }
}
