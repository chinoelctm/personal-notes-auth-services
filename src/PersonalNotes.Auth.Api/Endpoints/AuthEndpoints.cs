using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Mapster;
using MediatR;
using PersonalNotes.Auth.Api.Cookies;
using PersonalNotes.Auth.Api.Models.Login;
using PersonalNotes.Auth.Application.Commands.Auth.Login;
using PersonalNotes.Auth.Application.Commands.Auth.Logout;
using PersonalNotes.Auth.Application.Commands.RefreshToken;
using PersonalNotes.Auth.Application.Queries.Users.GetCurrent;
using PersonalNotes.Auth.Domain.Exceptions;

namespace PersonalNotes.Auth.Api.Endpoints
{
    public static class AuthEndpoints
    {
        public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/v1/auth")
                .WithTags("Auth")
                .WithOpenApi();

            group.MapPost("/login", LoginAsync)
                .WithSummary("Valida usuario y contraseña y deja la sesión en cookies HttpOnly (auth_token 15 min, refresh_token 7 días). Devuelve el usuario. 401 credenciales inválidas, 423 cuenta bloqueada.");
            group.MapPost("/refresh", RefreshTokenAsync)
                .WithSummary("Rota el refresh_token de la cookie y renueva ambas cookies. 204, o 401 (y borra las cookies) si la sesión no es válida.");
            group.MapPost("/logout", LogoutAsync)
                .WithSummary("Revoca el refresh_token de la cookie y borra las cookies. Siempre 204.");
            group.MapGet("/me", GetCurrentUserAsync)
                .RequireAuthorization()
                .WithSummary("Usuario de la sesión (según la cookie auth_token). 401 si no hay sesión o expiró.");

            return app;
        }

        private static async Task<IResult> LoginAsync(LoginRequest request, IMediator mediator, AuthCookieWriter cookies,
            HttpResponse response, CancellationToken ct)
        {
            var command = request.Adapt<LoginCommand>();
            var result = await mediator.Send(command, ct);

            cookies.Write(response, result.Tokens);
            return Results.Ok(result.User);
        }

        private static async Task<IResult> RefreshTokenAsync(HttpRequest request, IMediator mediator, AuthCookieWriter cookies,
            HttpResponse response, CancellationToken ct)
        {
            var refreshToken = request.Cookies[cookies.RefreshTokenName];
            if (string.IsNullOrWhiteSpace(refreshToken))
                throw new InvalidRefreshTokenException("No hay una sesión activa.");

            try
            {
                var tokens = await mediator.Send(new RefreshTokenCommand(refreshToken), ct);
                cookies.Write(response, tokens);
                return Results.NoContent();
            }
            catch (InvalidRefreshTokenException ex)
            {
                // Se responde aquí y no en GlobalExceptionHandler: el middleware de excepciones limpia los
                // headers, y con ellos las cookies que se están borrando
                cookies.Clear(response);
                return Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Sesión expirada", detail: ex.Message);
            }
        }

        private static async Task<IResult> LogoutAsync(HttpRequest request, IMediator mediator, AuthCookieWriter cookies,
            HttpResponse response, CancellationToken ct)
        {
            await mediator.Send(new LogoutCommand(request.Cookies[cookies.RefreshTokenName]), ct);

            cookies.Clear(response);
            return Results.NoContent();
        }

        private static async Task<IResult> GetCurrentUserAsync(ClaimsPrincipal principal, IMediator mediator, CancellationToken ct)
        {
            var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(sub, out var userId))
                throw new UnauthorizedException("La sesión no es válida.");

            var result = await mediator.Send(new GetCurrentUserQuery(userId), ct);
            return Results.Ok(result);
        }
    }
}
