using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MinecraftServerDiscordBot.Services;

/// <summary>
/// Service for checking user permissions based on Discord roles
/// </summary>
public class PermissionService
{
    private readonly ILogger<PermissionService> logger;
    private readonly DiscordBotOptions options;

    public PermissionService(
        ILogger<PermissionService> logger,
        IOptions<DiscordBotOptions> options)
    {
        this.logger = logger;
        this.options = options.Value;
    }

    /// <summary>
    /// Check if a user has permission to execute a command based on their roles
    /// </summary>
    public bool HasPermission(SocketSlashCommand command, CommandCategory category)
    {
        // If RBAC is disabled, allow all commands
        if (!options.RolePermissions.EnableRoleChecks)
        {
            return true;
        }

        if (command.User is not SocketGuildUser user)
        {
            if(logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning("User {Username} is not a guild member, denying permission", command.User.Username);
            }

            return false;
        }

        // Check administrator bypass
        if (options.RolePermissions.AdministratorBypass && user.GuildPermissions.Administrator)
        {
            if(logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("User {Username} has Administrator permission, bypassing role checks", user.Username);
            }

            return true;
        }

        var allowedRoles = GetAllowedRolesForCategory(category);

        // If no roles configured, deny access (when RBAC is enabled)
        if (allowedRoles.Count == 0)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("No roles configured for category {Category}, denying access", category);
            }
            return false;
        }

        // Check for @everyone
        if (allowedRoles.Any(r => r.Equals("@everyone", StringComparison.OrdinalIgnoreCase)))
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("@everyone role configured for category {Category}, allowing access", category);
            }

            return true;
        }

        // Get user's roles
        var userRoles = user.Roles.ToList();
        
        // Check if user has any of the allowed roles
        foreach (var allowedRole in allowedRoles)
        {
            // Try to match by role ID
            if (ulong.TryParse(allowedRole, out var roleId))
            {
                if (userRoles.Any(r => r.Id == roleId))
                {
                    if (logger.IsEnabled(LogLevel.Debug))
                    {
                        logger.LogDebug("User {Username} has role ID {RoleId} for category {Category}", 
                        user.Username, roleId, category);
                    }

                    return true;
                }
            }
            // Try to match by role name (case-insensitive)
            else
            {
                if (userRoles.Any(r => r.Name.Equals(allowedRole, StringComparison.OrdinalIgnoreCase)))
                {
                    if (logger.IsEnabled(LogLevel.Debug))
                    {
                        logger.LogDebug("User {Username} has role '{RoleName}' for category {Category}", 
                        user.Username, allowedRole, category);
                    }

                    return true;
                }
            }
        }
        if(logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("User {Username} does not have required roles for category {Category}. User roles: {UserRoles}", 
            user.Username, category, string.Join(", ", userRoles.Select(r => r.Name)));
        }

        return false;
    }

    /// <summary>
    /// Get the list of allowed roles for a command category
    /// </summary>
    private List<string> GetAllowedRolesForCategory(CommandCategory category)
    {
        return category switch
        {
            CommandCategory.Whitelist => options.RolePermissions.WhitelistRoles,
            CommandCategory.Player => options.RolePermissions.PlayerRoles,
            CommandCategory.Server => options.RolePermissions.ServerRoles,
            _ => []
        };
    }

    /// <summary>
    /// Get user-friendly error message for permission denial
    /// </summary>
    public string GetPermissionDeniedMessage(CommandCategory category)
    {
        var allowedRoles = GetAllowedRolesForCategory(category);
        
        if (allowedRoles.Count == 0)
        {
            return "? You do not have permission to use this command.";
        }

        var roleNames = string.Join(", ", allowedRoles.Select(r => $"**{r}**"));
        return $"? You need one of the following roles to use this command: {roleNames}";
    }
}

/// <summary>
/// Command category enumeration for permission checking
/// </summary>
public enum CommandCategory
{
    Whitelist,
    Player,
    Server
}
