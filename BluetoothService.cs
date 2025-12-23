using InTheHand.Net.Sockets;
using InTheHand.Net.Bluetooth;
using System.Collections.Concurrent;
using System.Text;
using System.Net.Sockets;
using System.Diagnostics;

namespace BTChat;

/// <summary>
/// Holds information about a discovered peer, including whether it's a compatible chat client.
/// </summary>
public record DiscoveredPeer(BluetoothDeviceInfo Info, bool IsChatPeer);

/// <summary>
/// Handles Bluetooth device scanning, discovery, and communication.
/// </summary>
public class BluetoothService
{
    public event EventHandler<BluetoothDeviceInfo>? PeerDiscovered;
    public event EventHandler<(string peer, string message)>? MessageReceived;

    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _scanLoopTask;
    private Task? _listenLoopTask;
    private BluetoothListener? _listener;
    private readonly ConcurrentDictionary<string, DiscoveredPeer> _discoveredPeers = new();

    // Per PRD: This unique ID allows our app to find other instances of itself.
    // You can generate your own at https://www.guidgenerator.com/
    private readonly Guid _serviceClassId = new("a9b7f6e4-0d76-4cce-9078-f3b55e49337c");

    private readonly TimeSpan _scanInterval = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Per PRD: Initiates peer discovery, broadcasts anonymous user presence, and listens for peers.
    /// The backend runs a 10-second looping scan.
    /// </summary>
    public Task StartAsync()
    {
        _cancellationTokenSource = new CancellationTokenSource();

        // Start the scanning and listening loops on background threads.
        _scanLoopTask = Task.Run(() => ScanLoopAsync(_cancellationTokenSource.Token));
        _listenLoopTask = Task.Run(() => ListenForConnections(_cancellationTokenSource.Token));

        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops the scanning and listening processes and waits for them to shut down gracefully.
    /// </summary>
    public async Task StopAsync()
    {
        if (_cancellationTokenSource is { IsCancellationRequested: false })
        {
            _cancellationTokenSource.Cancel();
        }

        // Stop the listener to unblock the AcceptBluetoothClientAsync call in the listening loop.
        // This must be done *before* awaiting the task.
        _listener?.Stop();

        // Wait for the background tasks to complete.
        var tasks = new[] { _scanLoopTask, _listenLoopTask };
        await Task.WhenAll(tasks.Where(t => t != null).ToArray()!);

        Console.WriteLine("Bluetooth services stopped.");
    }

    /// <summary>
    /// Gets the local device's primary Bluetooth MAC address.
    /// </summary>
    public string? GetLocalDeviceAddress()
    {
        try
        {
            return BluetoothRadio.Default?.LocalAddress.ToString();
        }
        catch (PlatformNotSupportedException ex)
        {
            Console.WriteLine($"Bluetooth Error: Could not get local device address. {ex.Message}");
            return null;
        }
    }

    public IReadOnlyDictionary<string, DiscoveredPeer> GetDiscoveredPeers() => _discoveredPeers;

    private async Task ScanLoopAsync(CancellationToken token)
    {
        Console.WriteLine("Starting Bluetooth scan loop...");
        while (!token.IsCancellationRequested)
        {
            try
            {
                // DiscoverDevices is a blocking call; run it in a task and ensure the client is disposed.
                using var client = new BluetoothClient();
                var peers = await Task.Run(() => client.DiscoverDevices(), token);

                foreach (var peer in peers)
                {
                    if (string.IsNullOrEmpty(peer.DeviceName) || _discoveredPeers.ContainsKey(peer.DeviceName))
                    {
                        continue; // Skip unnamed devices or peers we've already found.
                    }

                    // Check if the peer is running our chat service, but don't filter it out.
                    // We want to display all devices.
                    bool hasService = false;
                    try
                    {
                        // This check can be slow, so we keep it in the background task.
                        hasService = await Task.Run(() => peer.InstalledServices.Contains(_serviceClassId), token);
                    }
                    catch (Exception ex)
                    {
                        // This can happen if the device is no longer available. We can ignore it.
                        Debug.WriteLine($"Service discovery query failed for {peer.DeviceName}: {ex.Message}");
                    }
                    var discoveredPeer = new DiscoveredPeer(peer, hasService);
                    if (_discoveredPeers.TryAdd(peer.DeviceName, discoveredPeer))
                    {
                        PeerDiscovered?.Invoke(this, peer);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // This is expected when the token is cancelled.
                break;
            }
            catch (PlatformNotSupportedException)
            {
                Console.WriteLine("Bluetooth Error: The platform does not support Bluetooth.");
                break; // Stop the loop if Bluetooth is not supported.
            }
            catch (Exception ex)
            {
                // Per PRD: If Bluetooth unavailable or permissions denied, backend surfaces errors.
                Console.WriteLine($"Bluetooth Scan Error: {ex.Message}. Is Bluetooth turned on?");
            }

            await Task.Delay(_scanInterval, token);
        }
        Console.WriteLine("Bluetooth scan loop stopped.");
    }

    private async Task ListenForConnections(CancellationToken token)
    {
        try
        {
            _listener = new BluetoothListener(_serviceClassId);
            _listener.Start();
            Console.WriteLine("Listener started. Waiting for connections...");

            while (!token.IsCancellationRequested)
            {
                // This call blocks until a connection is received or the listener is stopped.
                var client = await _listener.AcceptBluetoothClientAsync();
                var peerName = client.RemoteMachineName;
                Console.WriteLine($"Accepted connection from {peerName}");

                // Handle the client connection in a separate task to not block the listener.
                _ = Task.Run(async () =>
                {
                    using var stream = client.GetStream();
                    using var reader = new StreamReader(stream, Encoding.UTF8);
                    try
                    {
                        // Read messages until the stream is closed or cancelled.
                        while (client.Connected && !token.IsCancellationRequested)
                        {
                            var message = await reader.ReadLineAsync(token);
                            if (message != null)
                            {
                                MessageReceived?.Invoke(this, (peerName, message));
                            }
                        }
                    }
                    catch (OperationCanceledException) { /* Expected on shutdown */ }
                    catch (IOException ex) { Console.WriteLine($"Connection lost with {peerName}: {ex.Message}"); }
                }, token);
            }
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.Interrupted)
        {
            // This exception is expected when listener.Stop() is called.
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Bluetooth Listener Error: {ex.Message}");
        }
        finally
        {
            _listener?.Stop(); // Ensure listener is stopped on exit.
            Console.WriteLine("Bluetooth listener stopped.");
        }
    }

    public async Task SendMessageAsync(string deviceName, string message)
    {
        if (!_discoveredPeers.TryGetValue(deviceName, out var discoveredPeer))
        {
            Console.WriteLine($"Error: Peer '{deviceName}' not found or not in range.");
            return;
        }

        var peerInfo = discoveredPeer.Info;

        try
        {
            using var client = new BluetoothClient();
            Console.WriteLine($"Attempting to connect to {peerInfo.DeviceName}...");
            await client.ConnectAsync(peerInfo.DeviceAddress, _serviceClassId);

            using var stream = client.GetStream();
            using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
            await writer.WriteLineAsync(message);
            Console.WriteLine("Message sent.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send message: {ex.Message}");
        }
    }
}