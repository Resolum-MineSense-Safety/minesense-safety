# syntax=docker/dockerfile:1
# Build context: src/backend (so the Shared kernel project can be copied)
#   docker build -f src/backend/IdentityAccessService/Dockerfile src/backend

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first to cache NuGet packages between builds
COPY Shared/MineSenseSafety.Shared.csproj Shared/
COPY IdentityAccessService/IdentityAccessService.csproj IdentityAccessService/
RUN dotnet restore IdentityAccessService/IdentityAccessService.csproj

COPY Shared/ Shared/
COPY IdentityAccessService/ IdentityAccessService/
RUN dotnet publish IdentityAccessService/IdentityAccessService.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_RUNNING_IN_CONTAINER=true

COPY --from=build /app/publish .

# The aspnet:8.0 image ships a non-root "app" user (UID 1654)
USER $APP_UID
EXPOSE 8080

ENTRYPOINT ["dotnet", "IdentityAccessService.dll"]
