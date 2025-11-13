# Minecraft Discord Bot

A Discord bot for managing a Minecraft server via RCON commands. Built with ASP.NET Core 10.0 and Discord.Net.

## Features

- **Whitelist Management**: Add/remove players from the Minecraft server whitelist
- **Player Tracking**: View online players and whitelisted players
- **Server Info**: Get Minecraft server version information
- **Command Categories**: Enable/disable command groups via configuration
- **Role-Based Access Control (RBAC)**: Restrict commands based on Discord roles
- **Health Monitoring**: Built-in health check endpoint for container orchestration
- **Channel Restrictions**: Optionally restrict commands to specific Discord channels
- **RCON Resilience**: Automatic reconnection to Minecraft server with lazy connection and idle timeout

## RCON Connection Behavior

The bot uses a smart RCON connection strategy:

- **Lazy Connection**: The bot doesn't connect to RCON at startup. It connects only when the first Discord command is executed.
- **Automatic Reconnection**: If the Minecraft server is offline when the bot starts or when a command is executed, the bot will show an error but will automatically retry on the next command.
- **Idle Timeout**: After 60 seconds of inactivity, the RCON connection is automatically closed to free resources.
- **Connection Pooling**: The connection is reused for multiple commands within the 60-second window.

This design ensures:
- The bot can start even if the Minecraft server is offline
- The bot automatically reconnects if the Minecraft server restarts
- Resources are efficiently managed with automatic connection cleanup
- Users get clear error messages if the server is unavailable

## Commands

### Whitelist Commands
- `/whitelist-add <player>` - Add a player to the whitelist
- `/whitelist-remove <player>` - Remove a player from the whitelist
- `/whitelist-list` - List all whitelisted players

### Player Commands
- `/players-online` - Show currently online players

### Server Commands
- `/version` - Display server version

> **Note**: Command names can be customized with a prefix via configuration (e.g., `/mc-whitelist-add` instead of `/whitelist-add`). See [Command Prefix](#command-prefix) below.
> 
> **Note**: Command categories can be enabled or disabled via configuration. See [Command Categories Configuration](#command-categories) below.
> 
> **RBAC**: Commands can be restricted to specific Discord roles. See [Role-Based Access Control](#role-based-access-control) below.

## Configuration

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `DISCORD_BOT_TOKEN` | Discord bot token (required) | - |
| `ALLOWED_CHANNEL_ID` | Discord channel ID where commands are allowed | - |
| `COMMAND_PREFIX` | Prefix to add to all command names | - |
| `ENABLE_WHITELIST_COMMANDS` | Enable whitelist management commands | `true` |
| `ENABLE_PLAYER_COMMANDS` | Enable player information commands | `true` |
| `ENABLE_SERVER_COMMANDS` | Enable server information commands | `true` |
| `ENABLE_RBAC` | Enable role-based access control | `false` |
| `RBAC_ADMIN_BYPASS` | Allow Discord admins to bypass role checks | `true` |
| `WHITELIST_ROLES` | Roles allowed to use whitelist commands | - |
| `PLAYER_ROLES` | Roles allowed to use player commands | - |
| `SERVER_ROLES` | Roles allowed to use server commands | - |
| `RCON_HOST` | Minecraft server hostname | `minecraft` |
| `RCON_PORT` | RCON port | `25575` |
| `RCON_PASSWORD` | RCON password (required) | - |
| `ASPNETCORE_URLS` | HTTP listener URLs | `http://+:8080` |

### Command Prefix

Add a custom prefix to all bot commands to avoid conflicts with other bots or to brand your commands.

**Configuration via appsettings.json:**
```json
{
  "Discord": {
    "BotToken": "your_discord_bot_token",
    "CommandPrefix": "mc"
  }
}
```

**Configuration via Environment Variables:**
```bash
COMMAND_PREFIX=mc
```

**Examples:**
- `CommandPrefix = ""` (empty/default): `/whitelist-add`, `/players-online`, `/version`
- `CommandPrefix = "mc"`: `/mc-whitelist-add`, `/mc-players-online`, `/mc-version`
- `CommandPrefix = "myserver"`: `/myserver-whitelist-add`, `/myserver-players-online`, `/myserver-version`

**Use Cases:**
- **Multiple bots**: Prevent command name conflicts with other Discord bots
- **Branding**: Add your server name to commands (e.g., `survival`, `creative`)
- **Organization**: Group commands by server or purpose
- **Multiple Minecraft servers**: Run multiple bot instances with different prefixes

### Command Categories

Control which command groups are available to users. This is useful for:
- **Security**: Disable admin commands in public channels
- **Customization**: Only enable commands you need
- **Foundation for RBAC**: Works together with role-based permissions

**Configuration via appsettings.json:**
```json
{
  "Discord": {
    "BotToken": "your_discord_bot_token",
    "AllowedChannelIds": [],
    "CommandCategories": {
      "EnableWhitelist": true,
      "EnablePlayer": true,
      "EnableServer": true
    }
  }
}
```

**Configuration via Environment Variables:**
```bash
ENABLE_WHITELIST_COMMANDS=true
ENABLE_PLAYER_COMMANDS=true
ENABLE_SERVER_COMMANDS=false
```

For detailed information, see [COMMAND_CATEGORIES.md](COMMAND_CATEGORIES.md).

### Role-Based Access Control

Control which Discord roles can execute which command categories. Perfect for:
- **Security**: Restrict admin commands to specific roles
- **Tiered Access**: Different permissions for different roles
- **Public Bots**: Allow info commands for everyone, restrict management

**Quick Example:**
```bash
# Enable RBAC
ENABLE_RBAC=true

# Configure role permissions
WHITELIST_ROLES=Admin,Moderator
PLAYER_ROLES=@everyone
SERVER_ROLES=@everyone
```

**Key Features:**
- Match by role name (case-insensitive) or role ID
- Use `@everyone` to allow all users
- Administrator bypass option
- Multiple roles per command category
- Granular control per command type

**Configuration via appsettings.json:**
```json
{
  "Discord": {
    "RolePermissions": {
      "EnableRoleChecks": true,
      "AdministratorBypass": true,
      "WhitelistRoles": ["Admin", "Moderator"],
      "PlayerRoles": ["@everyone"],
      "ServerRoles": ["@everyone"]
    }
  }
}
```

For comprehensive documentation, see [RBAC_GUIDE.md](RBAC_GUIDE.md).

### appsettings.json

```json
{
  "Discord": {
    "BotToken": "your_discord_bot_token",
    "AllowedChannelIds": [],
    "CommandPrefix": "",
    "CommandCategories": {
      "EnableWhitelist": true,
      "EnablePlayer": true,
      "EnableServer": true
    },
    "RolePermissions": {
      "EnableRoleChecks": false,
      "AdministratorBypass": true,
      "WhitelistRoles": [],
      "PlayerRoles": [],
      "ServerRoles": []
    }
  },
  "Rcon": {
    "Host": "minecraft",
    "Port": 25575,
    "Password": "your_rcon_password"
  }
}
```

## Running with Docker Compose

### Prerequisites

1. Docker and Docker Compose installed
2. A Discord bot token ([Create one here](https://discord.com/developers/applications))
3. Minecraft server with RCON enabled

### Setup

1. Create a `.env` file in the project root:

```bash
DISCORD_BOT_TOKEN=your_discord_bot_token_here
RCON_PASSWORD=your_rcon_password_here
RCON_HOST=minecraft
ALLOWED_CHANNEL_ID=123456789012345678

# Optional: Add command prefix
COMMAND_PREFIX=

# Optional: Disable specific command categories
ENABLE_WHITELIST_COMMANDS=true
ENABLE_PLAYER_COMMANDS=true
ENABLE_SERVER_COMMANDS=true

# Optional: Enable RBAC
ENABLE_RBAC=false
RBAC_ADMIN_BYPASS=true
WHITELIST_ROLES=Admin,Moderator
PLAYER_ROLES=@everyone
SERVER_ROLES=@everyone
```

2. Update your Minecraft server's `server.properties`:

```properties
enable-rcon=true
rcon.port=25575
rcon.password=your_rcon_password_here
```

3. Start the services:

```bash
docker compose up -d
```

### Example docker-compose.yml with Minecraft Server

```yaml
services:
  minecraft:
    image: itzg/minecraft-server:latest
    container_name: minecraft_server
    environment:
      EULA: "TRUE"
      TYPE: "PAPER"
      MEMORY: "2G"
      ENABLE_RCON: "true"
      RCON_PASSWORD: "${RCON_PASSWORD}"
      RCON_PORT: 25575
    ports:
      - "25565:25565"
    volumes:
      - minecraft_data:/data
    networks:
      - minecraft_network
    restart: unless-stopped

  minecraft_discord_bot:
    build:
      context: .
      dockerfile: MinecraftServerDiscordBot/Dockerfile
    container_name: minecraft_discord_bot
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - Discord__BotToken=${DISCORD_BOT_TOKEN}
      - Discord__AllowedChannelIds__0=${ALLOWED_CHANNEL_ID}
      - Discord__CommandPrefix=${COMMAND_PREFIX}
      - Discord__CommandCategories__EnableWhitelist=${ENABLE_WHITELIST_COMMANDS:-true}
      - Discord__CommandCategories__EnablePlayer=${ENABLE_PLAYER_COMMANDS:-true}
      - Discord__CommandCategories__EnableServer=${ENABLE_SERVER_COMMANDS:-true}
      - Discord__RolePermissions__EnableRoleChecks=${ENABLE_RBAC:-false}
      - Discord__RolePermissions__AdministratorBypass=${RBAC_ADMIN_BYPASS:-true}
      - Discord__RolePermissions__WhitelistRoles__0=${WHITELIST_ROLES:-}
      - Discord__RolePermissions__PlayerRoles__0=${PLAYER_ROLES:-}
      - Discord__RolePermissions__ServerRoles__0=${SERVER_ROLES:-}
      - Rcon__Host=minecraft
      - Rcon__Port=25575
      - Rcon__Password=${RCON_PASSWORD}
    ports:
      - "8080:8080"
    depends_on:
      - minecraft
    networks:
      - minecraft_network
    healthcheck:
      test: ["CMD-SHELL", "curl -f http://localhost:8080/health || exit 1"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
    restart: unless-stopped

networks:
  minecraft_network:
    driver: bridge

volumes:
  minecraft_data:
```

## Development

### Running Locally

1. Set up user secrets:

```bash
dotnet user-secrets set "Discord:BotToken" "your_token_here"
dotnet user-secrets set "Rcon:Password" "your_password_here"

# Optional: Configure command prefix
dotnet user-secrets set "Discord:CommandPrefix" "mc"

# Optional: Configure command categories
dotnet user-secrets set "Discord:CommandCategories:EnableWhitelist" "true"
dotnet user-secrets set "Discord:CommandCategories:EnablePlayer" "true"
dotnet user-secrets set "Discord:CommandCategories:EnableServer" "false"

# Optional: Configure RBAC
dotnet user-secrets set "Discord:RolePermissions:EnableRoleChecks" "true"
dotnet user-secrets set "Discord:RolePermissions:WhitelistRoles:0" "Admin"
dotnet user-secrets set "Discord:RolePermissions:WhitelistRoles:1" "Moderator"
```

2. Run the application:

```bash
cd MinecraftServerDiscordBot
dotnet run
```

### Building

```bash
dotnet build
```

### Publishing

```bash
dotnet publish -c Release
```

## Health Check

The bot exposes a health check endpoint at `/health` that returns:

- **Healthy (200)**: Discord bot is connected and ready
- **Degraded (200)**: Discord bot is running but not connected

Example response:
```json
{
  "status": "Healthy",
  "results": {
    "discord_bot": {
      "status": "Healthy",
      "description": "Discord bot is connected and ready."
    }
  }
}
```

## Logging

Logs are written to stdout and include:
- Discord.Net connection events
- Command executions (with category status)
- RCON connection status
- Enabled command categories on startup
- RBAC configuration and permission checks
- Errors and warnings

Example startup logs:
```
[Information] Command categories enabled - Whitelist: True, Player: True, Server: False
[Information] Role-Based Access Control (RBAC) is ENABLED
[Information] Administrator bypass: True
[Information] Whitelist roles: Admin, Moderator
[Information] Player roles: @everyone
[Information] Whitelist commands enabled for guild 123456789
[Information] Player commands enabled for guild 123456789
[Information] Registered 4 commands in guild 123456789
```

View logs with Docker:
```bash
docker logs -f minecraft_discord_bot
```

## Security Considerations

1. **Never commit secrets**: Use environment variables or user secrets
2. **Channel restrictions**: Configure `AllowedChannelIds` to limit command access
3. **Command categories**: Disable admin commands in public channels
4. **Role-Based Access Control**: Use RBAC to restrict commands by Discord role
5. **Administrator bypass**: Consider if Discord admins should bypass role checks
6. **Network isolation**: Use Docker networks to isolate services
7. **RCON password**: Use a strong, unique password for RCON

## Troubleshooting

### Bot not responding to commands

1. Ensure the bot has been invited with the `applications.commands` scope
2. Check that the bot has appropriate permissions in your Discord server
3. Verify the bot token is correct
4. Check logs: `docker logs minecraft_discord_bot`
5. Verify command category is enabled in configuration
6. Check RBAC permissions if enabled

### RCON connection failed

**The bot will now automatically retry RCON connections**, so most connection issues are temporary. You'll see an error message in Discord, but the bot will reconnect on the next command attempt.

Common scenarios:
- **Minecraft server starting up**: The bot may start before Minecraft is ready. Simply wait for Minecraft to finish starting, then try the command again.
- **Minecraft server restart**: If you restart your Minecraft server, the bot will automatically reconnect on the next Discord command.
- **First command after idle**: The first command after 60 seconds of inactivity may take slightly longer as the connection is re-established.

To diagnose persistent connection issues:
1. Verify RCON is enabled in `server.properties`
2. Check RCON password matches between bot config and server
3. Ensure containers are on the same network (Docker)
4. Verify the RCON port (default: 25575) is correct
5. Check logs: `docker logs minecraft_discord_bot` for connection attempt details

Example log messages:
```
[Information] Creating new RCON client for minecraft:25575
[Information] Connecting to RCON server at minecraft:25575...
[Information] Successfully connected to RCON server
[Information] RCON connection idle for 60 seconds, disconnecting...
```

### Commands not registering

Commands are registered per-guild when:
- The bot first connects
- The bot joins a new guild

Wait a few moments after the bot starts, or restart the bot if needed.

### Command says "This command is currently disabled"

The command category has been disabled in configuration. Check your appsettings.json or environment variables:
- Whitelist commands: `EnableWhitelist` or `ENABLE_WHITELIST_COMMANDS`
- Player commands: `EnablePlayer` or `ENABLE_PLAYER_COMMANDS`
- Server commands: `EnableServer` or `ENABLE_SERVER_COMMANDS`

### Permission Denied Message

When RBAC is enabled, users need the appropriate roles:

1. **Check if RBAC is enabled**: Look for "RBAC is ENABLED" in logs
2. **Verify user has required role**: Check configured roles match user's roles
3. **Check administrator bypass**: If enabled, Discord admins bypass checks
4. **Review role configuration**: Ensure roles are spelled correctly (case-insensitive)

See [RBAC_GUIDE.md](RBAC_GUIDE.md) for detailed troubleshooting.

## Future Enhancements

- Per-command permission configuration
- Additional Minecraft commands (kick, ban, op, etc.)

## License

MIT License - Feel free to use and modify as needed.

## Contributing

Contributions are welcome! Please feel free to submit issues or pull requests.
