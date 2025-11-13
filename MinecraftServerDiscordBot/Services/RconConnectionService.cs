using Microsoft.Extensions.Logging;
using RCON.Core;
using RCON.Core.Interfaces;
using RCON.Core.Models;
using System.Diagnostics;

namespace MinecraftServerDiscordBot.Services;

/// <summary>
/// IRconClient implementation that manages connections with lazy connection and automatic timeout disconnection.
/// Connects to the Minecraft server when needed and disconnects after 60 seconds of inactivity.
/// </summary>
public class RconConnectionService : IRconClient
{
    private readonly ILogger<RconConnectionService> logger;
    private readonly string host;
    private readonly int port;
    private readonly string password;
    private readonly TimeSpan idleTimeout = TimeSpan.FromSeconds(60);
    
    private IRconClient? rconClient;
    private Timer? disconnectTimer;
    private readonly SemaphoreSlim connectionLock = new(1, 1);
    private DateTime lastActivity = DateTime.UtcNow;
    private bool disposed;

    public RconConnectionService(
        ILogger<RconConnectionService> logger,
        string host,
        int port,
        string password)
    {
        this.logger = logger;
        this.host = host;
        this.port = port;
        this.password = password;
    }

    /// <summary>
    /// Gets the current connection status.
    /// </summary>
    public bool IsConnected => rconClient?.IsConnected ?? false;

    /// <summary>
    /// Connects to the RCON server. This is called automatically when needed.
    /// </summary>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        await connectionLock.WaitAsync(cancellationToken);
        try
        {
            await EnsureConnectedAsync(cancellationToken);
        }
        finally
        {
            connectionLock.Release();
        }
    }

    /// <summary>
    /// Disconnects from the RCON server.
    /// </summary>
    public async Task DisconnectAsync()
    {
        await connectionLock.WaitAsync();
        try
        {
            if (rconClient != null)
            {
                logger.LogInformation("Manually disconnecting RCON client");
                rconClient.Dispose();
                rconClient = null;
            }
        }
        finally
        {
            connectionLock.Release();
        }
    }

    /// <summary>
    /// Sends a command to the RCON server without waiting for response.
    /// </summary>
    public async Task<RconResponse> SendCommandAsync(string command, CancellationToken cancellationToken = default)
    {
        await connectionLock.WaitAsync(cancellationToken);
        try
        {
            await EnsureConnectedAsync(cancellationToken);
            
            var response = await rconClient!.SendCommandAsync(command, cancellationToken);
            
            // Update last activity and reset disconnect timer
            lastActivity = DateTime.UtcNow;
            ResetDisconnectTimer();

            return response;
        }
        finally
        {
            connectionLock.Release();
        }
    }

    /// <summary>
    /// Executes an RCON command, automatically connecting if needed.
    /// </summary>
    public async Task<string> ExecuteCommandAsync(string command, CancellationToken cancellationToken = default)
    {
        await connectionLock.WaitAsync(cancellationToken);
        try
        {
            await EnsureConnectedAsync(cancellationToken);
            
            var result = await rconClient!.ExecuteCommandAsync(command, cancellationToken);
            
            // Update last activity and reset disconnect timer
            lastActivity = DateTime.UtcNow;
            ResetDisconnectTimer();
            
            return result;
        }
        finally
        {
            connectionLock.Release();
        }
    }

    /// <summary>
    /// Executes a typed RCON command, automatically connecting if needed.
    /// </summary>
    public async Task<T> ExecuteCommandAsync<T>(ICommand<T> command, CancellationToken cancellationToken = default)
    {
        await connectionLock.WaitAsync(cancellationToken);
        try
        {
            await EnsureConnectedAsync(cancellationToken);
            
            var result = await rconClient!.ExecuteCommandAsync(command, cancellationToken);
            
            // Update last activity and reset disconnect timer
            lastActivity = DateTime.UtcNow;
            ResetDisconnectTimer();
            
            return result;
        }
        finally
        {
            connectionLock.Release();
        }
    }

    /// <summary>
    /// Ensures the RCON client is connected. Creates and connects if needed.
    /// </summary>
    private async Task EnsureConnectedAsync(CancellationToken cancellationToken = default)
    {
        // Check if we need to create a new client
        if (rconClient == null)
        {
            if(logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Creating new RCON client for {Host}:{Port}", host, port);
            }

            rconClient = new RconClientBuilder()
                .WithHost(host)
                .WithPort(port)
                .WithPassword(password)
                .WithTimeout(5000)
                .Build();
        }

        // Check if we're already connected
        if (rconClient.IsConnected)
        {
            logger.LogDebug("RCON client already connected");
            return;
        }

        // Attempt to connect
        if(logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Connecting to RCON server at {Host}:{Port}...", host, port);
        }

        try
        {
            await rconClient.ConnectAsync(cancellationToken);
            logger.LogInformation("Successfully connected to RCON server");
            
            // Start disconnect timer
            ResetDisconnectTimer();
        }
        catch (Exception ex)
        {
            if(logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "Failed to connect to RCON server at {Host}:{Port}. The Minecraft server may not be running yet.", host, port);
            }

            // Dispose the failed client so we create a fresh one next time
            rconClient?.Dispose();
            rconClient = null;
            
            throw new InvalidOperationException($"Failed to connect to Minecraft RCON server at {host}:{port}. Is the server running?", ex);
        }
    }

    /// <summary>
    /// Resets the disconnect timer to disconnect after idle timeout.
    /// </summary>
    private void ResetDisconnectTimer()
    {
        disconnectTimer?.Dispose();
        disconnectTimer = new Timer(
            async _ =>
            {
                if (disposed) return; 
                await DisconnectIfIdleAsync();
            },
            null,
            idleTimeout,
            Timeout.InfiniteTimeSpan);
    }

    /// <summary>
    /// Disconnects from RCON if the connection has been idle for the timeout period.
    /// </summary>
    private async Task DisconnectIfIdleAsync()
    {
        await connectionLock.WaitAsync();
        try
        {
            var idleTime = DateTime.UtcNow - lastActivity;
            
            if (idleTime >= idleTimeout && rconClient?.IsConnected == true)
            {
                if(logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("RCON connection idle for {IdleSeconds} seconds, disconnecting...", 
                    idleTimeout.TotalSeconds);
                }

                try
                {
                    rconClient.Dispose();
                    rconClient = null;
                    logger.LogInformation("RCON connection closed due to inactivity");
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Error while disconnecting RCON client");
                }
            }
        }
        finally
        {
            connectionLock.Release();
        }
    }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        
        disconnectTimer?.Dispose();
        disconnectTimer = null;
        
        rconClient?.Dispose();
        rconClient = null;
        
        connectionLock.Dispose();

        GC.SuppressFinalize(this);
    }
}
