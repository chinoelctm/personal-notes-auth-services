using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PersonalNotes.Auth.Api.Cookies;
using PersonalNotes.Auth.Api.Exceptions;
using PersonalNotes.Auth.Api.Options;
using PersonalNotes.Auth.Infrastructure.Options;
using PersonalNotes.Auth.Infrastructure.Services;

namespace PersonalNotes.Auth.Api.Extensions
{
    public static class PresentationExtensions
    {
        public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<AuthCookieOptions>()
                .Bind(configuration.GetSection(AuthCookieOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();
            services.AddSingleton<AuthCookieWriter>();

            services.AddCookieJwtAuthentication();

            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title       = "PersonalNotes.Auth API",
                    Version     = "v1",
                    Description = "Autenticación de Notas Personales: login con usuario y contraseña, sesión en cookies HttpOnly y rotación de refresh tokens"
                });
            });

            services.AddRouting(options => options.LowercaseUrls = true);
            services.AddExceptionHandler<GlobalExceptionHandler>();
            services.AddProblemDetails();

            return services;
        }

        /// <summary>
        /// Valida el access token de la cookie auth_token con la clave pública derivada de la privada (la misma
        /// validación que hace personal-notes-service con JwtSettings:PublicKeyPem).
        /// </summary>
        private static IServiceCollection AddCookieJwtAuthentication(this IServiceCollection services)
        {
            JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
            services.AddAuthorization();

            services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
                .Configure<IOptions<JwtOptions>, IOptions<AuthCookieOptions>>((options, jwt, cookies) =>
                {
                    var rsa = PemKeys.ImportRsa(jwt.Value.PrivateKeyPem, requirePrivateKey: true);
                    var publicKey = new RsaSecurityKey(rsa.ExportParameters(includePrivateParameters: false));

                    options.MapInboundClaims = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidIssuer = jwt.Value.Issuer,
                        ValidAudience = jwt.Value.Audience,
                        IssuerSigningKey = publicKey,
                        ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
                        ClockSkew = TimeSpan.FromSeconds(30)
                    };

                    var cookieName = cookies.Value.AccessTokenName;
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            context.Token = context.Request.Cookies[cookieName];
                            return Task.CompletedTask;
                        },
                        OnChallenge = async context =>
                        {
                            context.HandleResponse();
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            await context.Response.WriteAsJsonAsync(new ProblemDetails
                            {
                                Status = StatusCodes.Status401Unauthorized,
                                Title  = "No autorizado",
                                Detail = "La sesión expiró o no es válida."
                            });
                        }
                    };
                });

            return services;
        }
    }
}
