# syntax=docker/dockerfile:1
#
# Build context is the repo root (see docker-compose.yml's "context: .") so this can COPY
# every project the API references. No secret is ever baked into a layer here — the JWT
# secret, connection string, etc. are all supplied at container *run* time via environment
# variables (see docker-compose.yml / .env.example), never as a Dockerfile ARG/ENV default.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, using only the .csproj files, so this layer is cached across code-only changes.
COPY src/AIRecruiter.Domain/AIRecruiter.Domain.csproj src/AIRecruiter.Domain/
COPY src/AIRecruiter.Application/AIRecruiter.Application.csproj src/AIRecruiter.Application/
COPY src/AIRecruiter.Infrastructure/AIRecruiter.Infrastructure.csproj src/AIRecruiter.Infrastructure/
COPY src/AIRecruiter.API/AIRecruiter.API.csproj src/AIRecruiter.API/
RUN dotnet restore src/AIRecruiter.API/AIRecruiter.API.csproj

COPY src/ src/
RUN dotnet publish src/AIRecruiter.API/AIRecruiter.API.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "AIRecruiter.API.dll"]
