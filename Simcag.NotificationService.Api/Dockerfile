# Build context: raiz do repositório
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["shared/shared.csproj", "shared/"]
COPY ["notification-service/Simcag.NotificationService.Api/Simcag.NotificationService.Api.csproj", "notification-service/Simcag.NotificationService.Api/"]
COPY ["notification-service/Simcag.NotificationService.Application/Simcag.NotificationService.Application.csproj", "notification-service/Simcag.NotificationService.Application/"]
COPY ["notification-service/Simcag.NotificationService.Domain/Simcag.NotificationService.Domain.csproj", "notification-service/Simcag.NotificationService.Domain/"]
COPY ["notification-service/Simcag.NotificationService.Infrastructure/Simcag.NotificationService.Infrastructure.csproj", "notification-service/Simcag.NotificationService.Infrastructure/"]
RUN dotnet restore "notification-service/Simcag.NotificationService.Api/Simcag.NotificationService.Api.csproj"
COPY shared/ shared/
COPY notification-service/ notification-service/
RUN dotnet publish "notification-service/Simcag.NotificationService.Api/Simcag.NotificationService.Api.csproj" -c ${BUILD_CONFIGURATION} -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Simcag.NotificationService.Api.dll"]
