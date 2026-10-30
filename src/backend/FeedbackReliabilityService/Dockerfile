# syntax=docker/dockerfile:1
# feedback-reliability-service image (section 3.1). The build context is src/backend so the Shared kernel can be copied:
#   docker build -f src/backend/FeedbackReliabilityService/Dockerfile -t minesense/feedback-reliability-service src/backend

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first: the NuGet layer stays cached while only the source code changes.
COPY Shared/MineSenseSafety.Shared.csproj Shared/
COPY FeedbackReliabilityService/FeedbackReliabilityService.csproj FeedbackReliabilityService/
RUN dotnet restore FeedbackReliabilityService/FeedbackReliabilityService.csproj

COPY Shared/ Shared/
COPY FeedbackReliabilityService/ FeedbackReliabilityService/
RUN dotnet publish FeedbackReliabilityService/FeedbackReliabilityService.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# Ubuntu chiseled runtime: no shell or package manager and the non-root "app" user (UID 1654).
# The "extra" variant keeps ICU and tzdata so cultures and time zones behave as in development.
FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy-chiseled-extra AS runtime
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080     DOTNET_RUNNING_IN_CONTAINER=true

LABEL org.opencontainers.image.source="https://github.com/Resolum-MineSense-Safety/minesense-safety"       org.opencontainers.image.title="feedback-reliability-service"       org.opencontainers.image.vendor="Resolum"

COPY --from=build /app/publish .

USER $APP_UID
EXPOSE 8080

ENTRYPOINT ["dotnet", "FeedbackReliabilityService.dll"]
