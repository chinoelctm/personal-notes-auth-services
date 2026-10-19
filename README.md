# personal-notes-auth-services

Servicio de **autenticación** de Notas Personales. Valida usuario y contraseña, emite un access token JWT
(15 minutos) y un refresh token opaco (7 días) y los entrega en **cookies HttpOnly**. `personal-notes-service`
valida el access token de la cookie y `personal-notes-front` renueva la sesión sola cuando vence.

Replica la estructura y el funcionamiento de `Iam.Service` (WebInsigth), **sin OTP ni MFA**: las credenciales
correctas emiten los tokens directamente.

## Arquitectura

Clean Architecture en 4 proyectos (más los tests):

```
src/
├── PersonalNotes.Auth.Api/             → Minimal API, cookies, Swagger, workers
├── PersonalNotes.Auth.Application/     → Casos de uso (CQRS con MediatR)
├── PersonalNotes.Auth.Domain/          → Entidades, value objects, contratos y excepciones
└── PersonalNotes.Auth.Infrastructure/  → MongoDB, JWT RS256, BCrypt
tests/
└── PersonalNotes.Auth.Tests/           → xUnit
```

```
API (Endpoints) → MediatR → Application (Handlers) → Domain (Interfaces) → Infrastructure
```

Patrones (los mismos de Iam.Service): CQRS con MediatR, pipeline behaviors de validación (FluentValidation) y
logging, modelo de dominio rico (`User`, `RefreshSession`), value object `PasswordLockState`, repositorios sobre
el driver nativo de MongoDB, options con validación al arranque y `GlobalExceptionHandler` con ProblemDetails.

## Stack

| Área | Tecnología |
|---|---|
| Framework | .NET 8, ASP.NET Core Minimal API |
| Patrón | Clean Architecture + CQRS (MediatR) |
| Validación | FluentValidation |
| Mapeo | Mapster |
| Base de datos | MongoDB (Atlas en producción) |
| Tokens | JWT RS256 (RSA 2048) + refresh tokens opacos hasheados con SHA256 |
| Contraseñas | BCrypt (work factor 12) |
| Contenedor | Docker multistage (.NET 8 SDK → aspnet runtime) |
| Despliegue | Render (Web Service gratuito) |

## Endpoints

Base: `/api/v1`

| Método | Ruta | Descripción |
|---|---|---|
| `POST` | `/auth/login` | Body `{ "username", "password" }`. Deja las cookies y devuelve `{ id, username, name }`. 401 credenciales inválidas, 423 cuenta bloqueada |
| `POST` | `/auth/refresh` | Rota el `refresh_token` de la cookie y renueva ambas cookies. 204, o 401 (y borra las cookies) si la sesión no es válida |
| `POST` | `/auth/logout` | Revoca el `refresh_token` y borra las cookies. Siempre 204 |
| `GET`  | `/auth/me` | Usuario de la sesión según `auth_token`. 401 sin sesión |
| `GET`  | `/health` | Estado de la API y de MongoDB (503 si la base no responde) |

**Claims del JWT:** `sub` (id del usuario), `unique_name`, `name`, `jti`. Emisor `PersonalNotes.Auth`, audiencia
`PersonalNotes`.

## Cookies

Las mismas que arma el BFF de WebInsights a partir de la respuesta de Iam.Service:

| Cookie | Contenido | Path | Duración |
|---|---|---|---|
| `auth_token` | Access token JWT | `/` | 15 min (`AccessTokenMinutes`, máximo 15) |
| `refresh_token` | Refresh token opaco | `/api/v1/auth` | 7 días (`RefreshTokenDays`, máximo 7) |

Ambas son `HttpOnly` (el JavaScript del front nunca ve los tokens), `Secure` (solo HTTPS; `false` solo en el perfil
`http` local) y `SameSite=Strict`. El refresh token solo viaja a las rutas de auth, no en cada llamada a las notas.

**Mismo origen.** El front no llama a los servicios en otro dominio: en Render, el static site reenvía
`/api/v1/auth/*` a este servicio y el resto de `/api/*` a `personal-notes-service` (rewrites de su `render.yaml`);
en desarrollo lo hace `proxy.conf.json` de `ng serve`. Así las cookies son del propio sitio del front, el
navegador no las trata como de terceros y `SameSite=Strict` protege contra CSRF sin CORS.

## Seguridad

- **Rotación de refresh tokens:** cada `/refresh` marca el token como usado y emite uno nuevo; uno ya usado,
  revocado o vencido responde 401.
- **Solo hashes en la base:** la contraseña con BCrypt; el refresh token con SHA256 (es aleatorio de 256 bits).
- **Bloqueo por intentos:** 5 contraseñas incorrectas bloquean la cuenta 15 minutos (`LoginSettings`). A diferencia
  de Iam.Service, el bloqueo vence solo: no hay administrador que desbloquee.
- **Sin pistas de usuarios:** usuario inexistente y contraseña incorrecta responden igual y tardan lo mismo.
- **Firma asimétrica:** solo este servicio tiene la clave privada; `personal-notes-service` valida con la pública
  y no puede emitir tokens.
- **Limpieza diaria:** `SessionCleanupWorker` borra a las 23:00 (hora de Chile, `SessionSettings`) los refresh
  tokens vencidos, usados o revocados.

## Modelo de datos

Base `personal-notes-auth` (mismo cluster que las notas):

| Colección | Descripción | Índices |
|---|---|---|
| `users` | `username`, `normalizedUsername`, `name`, `password` (hash BCrypt), `enabled`, `passwordLock`, fechas | único `normalizedUsername` |
| `refreshSessions` | `userId`, `token` (hash SHA256), `createdAt`, `expiresAt`, `isUsed`, `usedAt`, `revokedAt` | único `token`; `userId`; `expiresAt` |

**Usuario inicial:** al arrancar se crea `SeedUser:Username` (`JoaquinMarquez`) con `SeedUser:PasswordHash` si no
existe. Si ya existe no se toca: cambiar el hash en la configuración no reemplaza la contraseña guardada.

## Configuración

`src/PersonalNotes.Auth.Api/appsettings.json`:

| Sección | Parámetros |
|---|---|
| `ConnectionStrings` | `MongoUri`, `MongoDb` (`personal-notes-auth`) |
| `JwtSettings` | `PrivateKeyPem` (**secreto**), `Issuer`, `Audience`, `AccessTokenMinutes` (15), `RefreshTokenDays` (7) |
| `LoginSettings` | `MaxAttempts` (5), `LockDurationMinutes` (15) |
| `SessionSettings` | `SessionCleanupTime` (`"23:00"`) |
| `SeedUser` | `Username`, `Name`, `PasswordHash` (**secreto**) |
| `AuthCookies` | `AccessTokenName`, `RefreshTokenName`, `RefreshTokenPath`, `Secure` |

Los secretos **no** van en el repo: en desarrollo con `dotnet user-secrets`, en Render como variables de entorno
(`JwtSettings__PrivateKeyPem`, `SeedUser__PasswordHash`). El PEM puede ir en una sola línea con `\n` literales.

### Generar una clave y un hash nuevos

```powershell
# Clave RSA 2048 (PowerShell 7): la privada para este servicio, la pública para personal-notes-service
$rsa = [System.Security.Cryptography.RSA]::Create(2048)
$rsa.ExportPkcs8PrivateKeyPem() | Set-Content jwt-private.pem
$rsa.ExportSubjectPublicKeyInfoPem() | Set-Content jwt-public.pem
```

El hash BCrypt se puede generar con cualquier herramienta BCrypt (work factor 12), por ejemplo
`BCrypt.Net.BCrypt.HashPassword("...", 12)`.

## Ejecución local

Requisitos: .NET 8 SDK y MongoDB en `localhost:27017` (o `ConnectionStrings:MongoUri` en user-secrets).

```bash
dotnet user-secrets set "JwtSettings:PrivateKeyPem" "$(cat jwt-private.pem)" --project src/PersonalNotes.Auth.Api
dotnet user-secrets set "SeedUser:PasswordHash" "<hash bcrypt>" --project src/PersonalNotes.Auth.Api
dotnet run --project src/PersonalNotes.Auth.Api --launch-profile http   # http://localhost:5090/swagger
dotnet test src/PersonalNotes.Auth.Service.sln
```

El front en `ng serve` (puerto 4200) reenvía `/api/v1/auth` a `localhost:5090` y el resto de `/api` a
`personal-notes-service` en `localhost:5080`.

## Despliegue en Render

El repo incluye `render.yaml`: **New → Blueprint** y elegir este repo. En el dashboard se ingresan
`ConnectionStrings__MongoUri` (la misma de Atlas que usa `personal-notes-service`), `JwtSettings__PrivateKeyPem` y
`SeedUser__PasswordHash`.

Si Render asigna una URL distinta de `personal-notes-auth-services.onrender.com`, actualiza el rewrite de
`/api/v1/auth/*` en el `render.yaml` del front.

## Diferencias con Iam.Service

| Iam.Service | personal-notes-auth-services |
|---|---|
| Login en dos pasos: credenciales + OTP por email (Service Bus) | Las credenciales emiten los tokens directo; sin OTP, MFA ni Service Bus |
| Usuarios multi-tenant por `customerId` y RUT | Un usuario identificado por `username` |
| Los tokens viajan en el body y el BFF los convierte en cookies | El propio servicio escribe las cookies (no hay BFF) |
| Bloqueo por contraseña permanente | Bloqueo de 15 minutos |
| Un login borra las sesiones anteriores del usuario | Cada dispositivo mantiene su sesión (PC y teléfono a la vez) |
| CRUD de usuarios y roles | Usuario inicial por configuración; sin CRUD |
| Fechas en hora de Chile | Fechas en UTC; la hora de Chile solo agenda la limpieza |

## Estructura

```
src/
├── PersonalNotes.Auth.Api/
│   ├── Program.cs
│   ├── Cookies/          (AuthCookieWriter)
│   ├── Endpoints/        (Auth, Health)
│   ├── Exceptions/       (GlobalExceptionHandler)
│   ├── Extensions/       (DI por capa + autenticación JWT por cookie)
│   ├── Models/           (LoginRequest)
│   ├── Options/          (AuthCookieOptions)
│   └── Workers/          (DatabaseInitializer, SessionCleanupWorker)
├── PersonalNotes.Auth.Application/
│   ├── Commands/         (Auth/Login, Auth/Logout, RefreshToken, Maintenance, Users/EnsureSeed)
│   ├── Queries/          (Users/GetCurrent, Health)
│   ├── Services/         (Auth/CredentialValidator, Tokens/TokenIssuer)
│   └── Behaviors/        (Validation, Logging)
├── PersonalNotes.Auth.Domain/
│   ├── Entities/         (User, RefreshSession)
│   ├── ValueObjects/     (PasswordLockState)
│   ├── Repositories/  Services/  Options/  Exceptions/
└── PersonalNotes.Auth.Infrastructure/
    ├── Context/          (AuthDbContext e índices)
    ├── Persistence/      (UserRepository, RefreshSessionRepository)
    ├── Services/         (JwtTokenService, PasswordHashingService, MongoDatabaseHealth)
    └── Options/
```
