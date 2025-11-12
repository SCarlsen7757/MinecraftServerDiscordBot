namespace MinecraftServerDiscordBot;

public class DiscordBotOptions
{
    public string BotToken { get; set; } = string.Empty;

    public HashSet<ulong> AllowedChannelIds { get; set; } = new();

    /// <summary>
    /// Prefix to add to all command names. Leave empty for no prefix.
    /// Example: "mc" will make commands like /mc-whitelist-add instead of /whitelist-add
    /// </summary>
    public string CommandPrefix { get; set; } = string.Empty;

    /// <summary>
    /// Configuration for enabled command categories
    /// </summary>
    public CommandCategoriesOptions CommandCategories { get; set; } = new();

    /// <summary>
    /// Role-based access control configuration for command categories
    /// </summary>
    public RolePermissionsOptions RolePermissions { get; set; } = new();
}

/// <summary>
/// Configuration for which command categories are enabled
/// </summary>
public class CommandCategoriesOptions
{
    /// <summary>
    /// Enable whitelist management commands (add, remove, list)
    /// </summary>
    public bool EnableWhitelist { get; set; } = true;

    /// <summary>
    /// Enable player management commands (list online players)
    /// </summary>
    public bool EnablePlayer { get; set; } = true;

    /// <summary>
    /// Enable server information commands (version)
    /// </summary>
    public bool EnableServer { get; set; } = true;
}

/// <summary>
/// Role-based permissions for command categories
/// </summary>
public class RolePermissionsOptions
{
    /// <summary>
    /// Enable role-based access control. If false, all users can use enabled commands.
    /// </summary>
    public bool EnableRoleChecks { get; set; } = false;

    /// <summary>
    /// Role names or IDs allowed to use whitelist commands.
    /// Empty list = all users allowed (when EnableRoleChecks is false).
    /// Use "@everyone" to allow all users explicitly.
    /// Can use role names (case-insensitive) or role IDs.
    /// </summary>
    public List<string> WhitelistRoles { get; set; } = [];

    /// <summary>
    /// Role names or IDs allowed to use player commands.
    /// Empty list = all users allowed (when EnableRoleChecks is false).
    /// Use "@everyone" to allow all users explicitly.
    /// Can use role names (case-insensitive) or role IDs.
    /// </summary>
    public List<string> PlayerRoles { get; set; } = [];

    /// <summary>
    /// Role names or IDs allowed to use server commands.
    /// Empty list = all users allowed (when EnableRoleChecks is false).
    /// Use "@everyone" to allow all users explicitly.
    /// Can use role names (case-insensitive) or role IDs.
    /// </summary>
    public List<string> ServerRoles { get; set; } = [];

    /// <summary>
    /// If true, users with Administrator permission bypass all role checks.
    /// </summary>
    public bool AdministratorBypass { get; set; } = true;
}
