# syntax=docker/dockerfile:1
# Build context: src/backend
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Shared/MineSenseSafety.Shared.csproj Shared/
COPY EdgeSynchronizationService/EdgeSynchronizationService.csproj EdgeSynchronizationService/
RUN dotnet restore EdgeSynchronizationService/EdgeSynchronizationService.csproj
COPY Shared/ Shared/
COPY EdgeSynchronizationService/ EdgeSynchronizationService/
RUN dotnet publish EdgeSynchronizationService/EdgeSynchronizationService.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_RUNNING_IN_CONTAINER=true
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "EdgeSynchronizationService.dll"]
