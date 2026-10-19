using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using PersonalNotes.Auth.Domain.Exceptions;

namespace PersonalNotes.Auth.Api.Exceptions
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
            => _logger = logger;

        public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
        {
            var (statusCode, message) = exception switch
            {
                InvalidRefreshTokenException => (401, exception.Message),
                UnauthorizedException        => (401, exception.Message),
                LockedException              => (423, exception.Message),
                NotFoundException            => (404, exception.Message),
                ConflictException            => (409, exception.Message),
                DomainException              => (400, exception.Message),
                ValidationException          => (400, "Uno o más campos no son válidos."),
                BadHttpRequestException      => (400, "El cuerpo o los parámetros de la solicitud no son válidos."),
                MongoConnectionException or TimeoutException => HandleDatabaseUnavailable(exception),
                _                            => HandleUnexpected(exception)
            };

            context.Response.StatusCode = statusCode;

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title  = GetTitle(exception),
                Detail = message
            };

            if (exception is ValidationException validationEx)
            {
                var errors = validationEx.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray());

                problemDetails.Extensions["errors"] = errors;
            }

            await context.Response
                .WriteAsJsonAsync(problemDetails, ct);

            return true;
        }

        private (int, string) HandleDatabaseUnavailable(Exception ex)
        {
            _logger.LogWarning(ex, "Base de datos no disponible: {Message}", ex.Message);
            return (503, "La base de datos no está disponible en este momento. Intenta de nuevo en unos segundos.");
        }

        private (int, string) HandleUnexpected(Exception ex)
        {
            _logger.LogError(ex, "Excepción inesperada: {Message}", ex.Message);
            return (500, "Ocurrió un error inesperado.");
        }

        private static string GetTitle(Exception exception) => exception switch
        {
            InvalidRefreshTokenException => "Sesión expirada",
            UnauthorizedException        => "No autorizado",
            LockedException              => "Cuenta bloqueada",
            NotFoundException            => "Recurso no encontrado",
            ConflictException            => "Conflicto de datos",
            ValidationException          => "Validación fallida",
            MongoConnectionException or TimeoutException => "Servicio no disponible",
            DomainException              => "Error de dominio",
            BadHttpRequestException      => "Solicitud inválida",
            _                            => "Error interno del servidor"
        };
    }
}
