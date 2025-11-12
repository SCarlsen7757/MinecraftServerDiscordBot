using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RCON.Commands.Minecraft.Java.Player;
using RCON.Commands.Minecraft.Java.Server;
using RCON.Commands.Minecraft.Java.Whitelist;
using RCON.Core.Interfaces;

namespace MinecraftServerDiscordBot.Services;

public class DiscordBotService : IHostedService
{
    private readonly ILogger<DiscordBotService> logger;
    private readonly DiscordBotOptions options;
    private readonly IServiceProvider serviceProvider;
    private readonly IRconClient rconClient;
    private readonly PermissionService permissionService;
    private DiscordSocketClient? client;
    private bool isConnected;

    public bool IsConnected => isConnected;

    public DiscordBotService(
        ILogger<DiscordBotService> logger,
        IOptions<DiscordBotOptions> options,
        IServiceProvider serviceProvider,
        IRconClient rconClient,
        PermissionService permissionService)
    {
        this.logger = logger;
        this.options = options.Value;
        this.serviceProvider = serviceProvider;
        this.rconClient = rconClient;
        this.permissionService = permissionService;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting Discord bot service...");

        if (string.IsNullOrWhiteSpace(options.BotToken))
        {
            logger.LogError("Discord bot token not configured. Bot will not start.");
            return;
        }

        // Log command prefix if configured
        if (!string.IsNullOrWhiteSpace(options.CommandPrefix))
        {
            logger.LogInformation("Command prefix configured: '{Prefix}' (commands will be like /{Prefix}-command-name)", 
                options.CommandPrefix, options.CommandPrefix);
        }

        // Log enabled command categories
        logger.LogInformation("Command categories enabled - Whitelist: {Whitelist}, Player: {Player}, Server: {Server}",
            options.CommandCategories.EnableWhitelist,
            options.CommandCategories.EnablePlayer,
            options.CommandCategories.EnableServer);

        // Log RBAC configuration
        if (options.RolePermissions.EnableRoleChecks)
        {
            logger.LogInformation("Role-Based Access Control (RBAC) is ENABLED");
            logger.LogInformation("Administrator bypass: {AdminBypass}", options.RolePermissions.AdministratorBypass);
            
            if (options.RolePermissions.WhitelistRoles.Count > 0)
                logger.LogInformation("Whitelist roles: {Roles}", string.Join(", ", options.RolePermissions.WhitelistRoles));
            
            if (options.RolePermissions.PlayerRoles.Count > 0)
                logger.LogInformation("Player roles: {Roles}", string.Join(", ", options.RolePermissions.PlayerRoles));
            
            if (options.RolePermissions.ServerRoles.Count > 0)
                logger.LogInformation("Server roles: {Roles}", string.Join(", ", options.RolePermissions.ServerRoles));
        }
        else
        {
            logger.LogInformation("Role-Based Access Control (RBAC) is DISABLED - all users can use enabled commands");
        }

        client = new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds,
            LogLevel = LogSeverity.Info
        });

        client.Log += LogAsync;
        client.Ready += OnReadyAsync;
        client.SlashCommandExecuted += OnSlashCommandExecutedAsync;
        client.JoinedGuild += OnJoinedGuildAsync;

        try
        {
            await client.LoginAsync(TokenType.Bot, options.BotToken);
            await client.StartAsync();
            
            logger.LogInformation("RCON configured for lazy connection - will connect when first command is executed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start Discord bot");
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopping Discord bot service...");

        if (client != null)
        {
            await client.LogoutAsync();
            await client.StopAsync();
            client.Dispose();
        }

        isConnected = false;
    }

    private Task LogAsync(LogMessage log)
    {
        var logLevel = log.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Verbose => LogLevel.Debug,
            LogSeverity.Debug => LogLevel.Trace,
            _ => LogLevel.Information
        };

        logger.Log(logLevel, log.Exception, "[Discord.Net] {Message}", log.Message);
        return Task.CompletedTask;
    }

    private async Task OnReadyAsync()
    {
        logger.LogInformation("Connected as {Username}#{Discriminator}", 
            client?.CurrentUser?.Username, client?.CurrentUser?.Discriminator);
        isConnected = true;

        if (client != null)
        {
            foreach (var guild in client.Guilds)
            {
                await RegisterCommandsForGuildAsync(guild);
            }
        }
    }

    private async Task OnJoinedGuildAsync(SocketGuild guild)
    {
        logger.LogInformation("Joined guild: {GuildName} (ID: {GuildId})", guild.Name, guild.Id);
        await RegisterCommandsForGuildAsync(guild);
    }

    private async Task RegisterCommandsForGuildAsync(SocketGuild guild)
    {
        var commands = new List<ApplicationCommandProperties>();

        // Whitelist commands
        if (options.CommandCategories.EnableWhitelist)
        {
            commands.Add(new SlashCommandBuilder()
                .WithName(GetCommandName("whitelist-add"))
                .WithDescription("Add a player to the Minecraft whitelist")
                .AddOption("player", ApplicationCommandOptionType.String, "Player name", isRequired: true)
                .Build());

            commands.Add(new SlashCommandBuilder()
                .WithName(GetCommandName("whitelist-remove"))
                .WithDescription("Remove a player from the Minecraft whitelist")
                .AddOption("player", ApplicationCommandOptionType.String, "Player name", isRequired: true)
                .Build());

            commands.Add(new SlashCommandBuilder()
                .WithName(GetCommandName("whitelist-list"))
                .WithDescription("List whitelisted players on the Minecraft server")
                .Build());

            logger.LogInformation("Whitelist commands enabled for guild {GuildId}", guild.Id);
        }

        // Player commands
        if (options.CommandCategories.EnablePlayer)
        {
            commands.Add(new SlashCommandBuilder()
                .WithName(GetCommandName("players-online"))
                .WithDescription("List online players on the Minecraft server")
                .Build());

            logger.LogInformation("Player commands enabled for guild {GuildId}", guild.Id);
        }

        // Server commands
        if (options.CommandCategories.EnableServer)
        {
            commands.Add(new SlashCommandBuilder()
                .WithName(GetCommandName("version"))
                .WithDescription("Get the Minecraft server version")
                .Build());

            logger.LogInformation("Server commands enabled for guild {GuildId}", guild.Id);
        }

        if (commands.Count == 0)
        {
            logger.LogWarning("No command categories are enabled. Bot will have no commands.");
            return;
        }

        foreach (var command in commands)
        {
            try
            {
                await guild.CreateApplicationCommandAsync(command);
                logger.LogDebug("Registered command '{CommandName}' in guild {GuildId}", 
                    command.Name, guild.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create command '{CommandName}' in guild {GuildId}", 
                    command.Name, guild.Id);
            }
        }

        logger.LogInformation("Registered {CommandCount} commands in guild {GuildId}", 
            commands.Count, guild.Id);
    }

    private async Task OnSlashCommandExecutedAsync(SocketSlashCommand command)
    {
        try
        {
            // Check if command is in allowed channel
            if (options.AllowedChannelIds.Count > 0 && 
                command.ChannelId != null && 
                !options.AllowedChannelIds.Contains(command.ChannelId.Value))
            {
                await command.RespondAsync(
                    "This command can only be used in the configured channel.", 
                    ephemeral: true);
                return;
            }

            // Get base command name (strip prefix if present)
            var baseCommandName = GetBaseCommandName(command.CommandName);

            // Check if the command category is enabled
            var (isCommandEnabled, category) = baseCommandName switch
            {
                "whitelist-add" or "whitelist-remove" or "whitelist-list" => 
                    (options.CommandCategories.EnableWhitelist, CommandCategory.Whitelist),
                "players-online" => 
                    (options.CommandCategories.EnablePlayer, CommandCategory.Player),
                "version" => 
                    (options.CommandCategories.EnableServer, CommandCategory.Server),
                _ => (false, (CommandCategory?)null)
            };

            if (!isCommandEnabled)
            {
                await command.RespondAsync(
                    "This command is currently disabled.", 
                    ephemeral: true);
                logger.LogWarning("Attempted to execute disabled command '{CommandName}' by user {Username}", 
                    command.CommandName, command.User.Username);
                return;
            }

            // Check role-based permissions
            if (category.HasValue && !permissionService.HasPermission(command, category.Value))
            {
                var deniedMessage = permissionService.GetPermissionDeniedMessage(category.Value);
                await command.RespondAsync(deniedMessage, ephemeral: true);
                logger.LogWarning("User {Username} denied access to command '{CommandName}' - insufficient permissions", 
                    command.User.Username, command.CommandName);
                return;
            }
            logger.LogInformation("Executing command '{CommandName}' from user {Username} in guild {GuildId}", 
                command.CommandName, command.User.Username, command.GuildId);

            await (baseCommandName switch
            {
                "whitelist-add" => HandleWhitelistAddAsync(command),
                "whitelist-remove" => HandleWhitelistRemoveAsync(command),
                "whitelist-list" => HandleWhitelistListAsync(command),
                "players-online" => HandlePlayersOnlineAsync(command),
                "version" => HandleVersionAsync(command),
                _ => command.RespondAsync($"Unknown command: {command.CommandName}", ephemeral: true)
            });
        }
        catch (TimeoutException)
        {
            logger.LogWarning("Timeout executing command '{CommandName}'", command.CommandName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing command '{CommandName}'", command.CommandName);
            
            try
            {
                if (!command.HasResponded)
                {
                    await command.RespondAsync($"Error: {ex.Message}", ephemeral: true);
                }
                else
                {
                    await command.FollowupAsync($"Error: {ex.Message}", ephemeral: true);
                }
            }
            catch
            {
                // Ignore if we can't send error response
            }
        }
    }

    private async Task HandleWhitelistAddAsync(SocketSlashCommand command)
    {
        var player = command.Data.Options.First().Value?.ToString();
        if (string.IsNullOrWhiteSpace(player))
        {
            await command.RespondAsync("Player name is required.", ephemeral: true);
            return;
        }

        var whitelistCommands = serviceProvider.GetRequiredService<WhitelistCommands>();
        var result = await whitelistCommands.AddPlayerAsync(player);
        
        await command.RespondAsync($"✅ Whitelist Add: {result.Status} - {result.Message}");
        logger.LogInformation("Whitelisted player '{Player}': {Status}", player, result.Status);
    }

    private async Task HandleWhitelistRemoveAsync(SocketSlashCommand command)
    {
        var player = command.Data.Options.First().Value?.ToString();
        if (string.IsNullOrWhiteSpace(player))
        {
            await command.RespondAsync("Player name is required.", ephemeral: true);
            return;
        }

        var whitelistCommands = serviceProvider.GetRequiredService<WhitelistCommands>();
        var result = await whitelistCommands.RemovePlayerAsync(player);
        
        await command.RespondAsync($"🗑️ Whitelist Remove: {result.Status} - {result.Message}");
        logger.LogInformation("Removed player '{Player}' from whitelist: {Status}", player, result.Status);
    }

    private async Task HandleWhitelistListAsync(SocketSlashCommand command)
    {
        var whitelistCommands = serviceProvider.GetRequiredService<WhitelistCommands>();
        var list = await whitelistCommands.GetPlayersAsync();
        
        var players = list.Players.Any() 
            ? string.Join('\n', list.Players.Select(p => $"• {p}")) 
            : "_(none)_";
        
        await command.RespondAsync($"**Whitelisted Players:**\n{players}");
    }

    private async Task HandlePlayersOnlineAsync(SocketSlashCommand command)
    {
        var playerCommands = serviceProvider.GetRequiredService<PlayerCommands>();
        var list = await playerCommands.GetPlayerListAsync();
        
        var players = list.Players.Any() 
            ? string.Join('\n', list.Players.Select(p => $"• {p.Name}")) 
            : "_(none)_";
        
        await command.RespondAsync($"**Online Players ({list.Players.Count()}):**\n{players}");
    }

    private async Task HandleVersionAsync(SocketSlashCommand command)
    {
        var serverCommands = serviceProvider.GetRequiredService<ServerCommands>();
        var version = await serverCommands.GetVersionAsync();
        
        await command.RespondAsync($"🎮 **Server Version:** {version.Id}");
    }

    /// <summary>
    /// Apply command prefix if configured
    /// </summary>
    private string GetCommandName(string baseName)
    {
        if (string.IsNullOrWhiteSpace(options.CommandPrefix))
        {
            return baseName;
        }
        return $"{options.CommandPrefix}-{baseName}";
    }

    /// <summary>
    /// Strip command prefix to get base command name
    /// </summary>
    private string GetBaseCommandName(string commandName)
    {
        if (string.IsNullOrWhiteSpace(options.CommandPrefix))
        {
            return commandName;
        }

        var prefix = $"{options.CommandPrefix}-";
        if (commandName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return commandName.Substring(prefix.Length);
        }

        return commandName;
    }
}
