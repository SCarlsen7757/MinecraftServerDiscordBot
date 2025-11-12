using Microsoft.Extensions.Diagnostics.HealthChecks;
using MinecraftServerDiscordBot.Services;

namespace MinecraftServerDiscordBot;

public class DiscordBotHealthCheck : IHealthCheck
{
    private readonly DiscordBotService botService;

    public DiscordBotHealthCheck(DiscordBotService botService)
    {
        this.botService = botService;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (botService.IsConnected)
        {
            return Task.FromResult(
                HealthCheckResult.Healthy("Discord bot is connected and ready."));
        }

        return Task.FromResult(
            HealthCheckResult.Degraded("Discord bot is not connected."));
    }
}
