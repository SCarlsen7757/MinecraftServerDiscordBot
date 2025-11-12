# Use ASP.NET runtime for apps that host Kestrel
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["MinecraftServerDiscordBot/MinecraftServerDiscordBot.csproj", "MinecraftServerDiscordBot/"]
RUN dotnet restore "./MinecraftServerDiscordBot/MinecraftServerDiscordBot.csproj"
COPY . .
WORKDIR "/src/MinecraftServerDiscordBot"
RUN dotnet build "./MinecraftServerDiscordBot.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MinecraftServerDiscordBot.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MinecraftServerDiscordBot.dll"]