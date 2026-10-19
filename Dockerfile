FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Copiar archivos de proyecto y restaurar dependencias
COPY src/PersonalNotes.Auth.Infrastructure/*.csproj ./PersonalNotes.Auth.Infrastructure/
COPY src/PersonalNotes.Auth.Api/*.csproj ./PersonalNotes.Auth.Api/
COPY src/PersonalNotes.Auth.Application/*.csproj ./PersonalNotes.Auth.Application/
COPY src/PersonalNotes.Auth.Domain/*.csproj ./PersonalNotes.Auth.Domain/

RUN dotnet restore PersonalNotes.Auth.Api/PersonalNotes.Auth.Api.csproj
COPY src/ ./

RUN dotnet publish PersonalNotes.Auth.Api/PersonalNotes.Auth.Api.csproj -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime-env
WORKDIR /app
COPY --from=build-env /app/out .

EXPOSE 8080

ENTRYPOINT ["dotnet", "PersonalNotes.Auth.Api.dll"]
