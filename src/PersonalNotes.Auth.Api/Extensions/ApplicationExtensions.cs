using FluentValidation;
using MediatR;
using PersonalNotes.Auth.Application.Behaviors;
using PersonalNotes.Auth.Application.Commands.Auth.Login;
using PersonalNotes.Auth.Application.Services.Auth;
using PersonalNotes.Auth.Application.Services.Tokens;

namespace PersonalNotes.Auth.Api.Extensions
{
    public static class ApplicationExtensions
    {
        public static IServiceCollection AddApplication(
            this IServiceCollection services)
        {
            services.AddMediatR(cfg =>
                cfg.RegisterServicesFromAssembly(typeof(LoginCommand).Assembly));

            services.AddValidatorsFromAssembly(
                typeof(LoginCommandValidator).Assembly);

            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

            services.AddScoped<ICredentialValidator, CredentialValidator>();
            services.AddScoped<ITokenIssuer, TokenIssuer>();

            return services;
        }
    }
}
