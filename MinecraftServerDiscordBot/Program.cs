using Discord;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MinecraftServerDiscordBot;
using MinecraftServerDiscordBot.Services;
using RCON.Core;
using RCON.Core.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Configure configuration sources
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddUserSecrets(typeof(Program).Assembly, optional: true)
    .AddEnvironmentVariables();

// Configure Discord bot options
var discordSection = builder.Configuration.GetSection("Discord");
builder.Services.Configure<DiscordBotOptions>(discordSection);

// Configure RCON connection service with lazy connection and auto-disconnect
var rconHost = builder.Configuration["Rcon:Host"] ?? "127.0.0.1";
var rconPort = int.TryParse(builder.Configuration["Rcon:Port"], out var p) ? p : 25575;
var rconPassword = builder.Configuration["Rcon:Password"] ?? string.Empty;

builder.Services.AddSingleton<IRconClient>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<RconConnectionService>>();
    return new RconConnectionService(logger, rconHost, rconPort, rconPassword);
});
builder.Services.AddTransient<RCON.Commands.Minecraft.Java.Whitelist.WhitelistCommands>();
builder.Services.AddTransient<RCON.Commands.Minecraft.Java.Player.PlayerCommands>();
builder.Services.AddTransient<RCON.Commands.Minecraft.Java.Server.ServerCommands>();

// Add permission service for RBAC
builder.Services.AddSingleton<PermissionService>();

// Add Discord bot as hosted service
builder.Services.AddSingleton<DiscordBotService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<DiscordBotService>());

builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck<DiscordBotHealthCheck>("discord_bot");

var app = builder.Build();

// Configure health endpoint
app.MapHealthChecks("/health");

app.Logger.LogInformation("Starting Minecraft Discord Bot...");
app.Logger.LogInformation("RCON will connect on-demand and disconnect after 60 seconds of inactivity");

await app.RunAsync();