FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Shared/MineSenseSafety.Shared.csproj Shared/
COPY FeedbackReliabilityService/FeedbackReliabilityService.csproj FeedbackReliabilityService/
RUN dotnet restore FeedbackReliabilityService/FeedbackReliabilityService.csproj
COPY Shared/ Shared/
COPY FeedbackReliabilityService/ FeedbackReliabilityService/
RUN dotnet publish FeedbackReliabilityService/FeedbackReliabilityService.csproj -c Release --no-restore -o /app/publish
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "FeedbackReliabilityService.dll"]
