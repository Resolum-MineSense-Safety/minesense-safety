# syntax=docker/dockerfile:1
# Monitoring Service image (section 3.1).
# Build context: src/backend
#   docker build -f src/backend/MonitoringService/Dockerfile -t minesense/monitoring-service src/backend

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first to cache NuGet packages between builds.
COPY Shared/MineSenseSafety.Shared.csproj Shared/
COPY MonitoringService/MonitoringService.csproj MonitoringService/
RUN dotnet restore MonitoringService/MonitoringService.csproj

COPY Shared/ Shared/
COPY MonitoringService/ MonitoringService/
RUN dotnet publish MonitoringService/MonitoringService.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# Hardened runtime: no shell or package manager.
# The extra variant includes ICU and tzdata.
FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy-chiseled-extra AS runtime
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_RUNNING_IN_CONTAINER=true

LABEL org.opencontainers.image.source="https://github.com/Resolum-MineSense-Safety/minesense-safety" \
      org.opencontainers.image.title="monitoring-service" \
      org.opencontainers.image.vendor="Resolum"

COPY --from=build /app/publish .

USER $APP_UID
EXPOSE 8080

ENTRYPOINT ["dotnet", "MonitoringService.dll"]
