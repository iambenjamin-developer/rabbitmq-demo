# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/runtime:8.0-alpine AS base
USER $APP_UID
WORKDIR /app


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Notification/Notification.Worker/Notification.Worker.csproj", "src/Notification/Notification.Worker/"]
COPY ["src/Shared/Shared.Contracts/Shared.Contracts.csproj", "src/Shared/Shared.Contracts/"]
COPY ["src/Shared/Shared.Core/Shared.Core.csproj", "src/Shared/Shared.Core/"]
COPY ["src/Notification/Notification.Application/Notification.Application.csproj", "src/Notification/Notification.Application/"]
COPY ["src/Notification/Notification.Domain/Notification.Domain.csproj", "src/Notification/Notification.Domain/"]
COPY ["src/Notification/Notification.Infrastructure/Notification.Infrastructure.csproj", "src/Notification/Notification.Infrastructure/"]
RUN dotnet restore "./src/Notification/Notification.Worker/Notification.Worker.csproj"
COPY . .
WORKDIR "/src/src/Notification/Notification.Worker"
RUN dotnet build "./Notification.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Notification.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Notification.Worker.dll"]