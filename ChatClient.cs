namespace BTChat;

using InTheHand.Net.Sockets;

/// <summary>
/// Orchestrates the core application logic, coordinating between services.
/// </summary>
public class ChatClient
{
    private readonly BluetoothService _bluetoothService;
    private readonly SessionManager _sessionManager;
    private readonly StorageService _storageService;

    /// <summary>
    /// Tracks the device name of the current chat partner to filter incoming messages for the UI.
    /// </summary>
    public string? CurrentChatPartner { get; set; }

    public ChatClient(BluetoothService bluetoothService, SessionManager sessionManager, StorageService storageService)
    {
        _bluetoothService = bluetoothService;
        _sessionManager = sessionManager;
        _storageService = storageService;

        // Subscribe to events from the Bluetooth service
        // It's crucial to handle these events to provide real-time feedback to the user.
        _bluetoothService.PeerDiscovered += OnPeerDiscovered;
        _bluetoothService.MessageReceived += OnMessageReceived;
    }

    public async Task StartAsync()
    {
        // To provide the best user experience, we start Bluetooth discovery first.
        // This allows the app to find peers while the other services initialize.
        await _bluetoothService.StartAsync();

        await _storageService.InitializeDatabaseAsync();
        InitializeSession(); // This will now show the user's MAC address
    }

    public void InitializeSession()
    {
        var macAddress = _bluetoothService.GetLocalDeviceAddress();
        if (macAddress != null)
        {
            _sessionManager.Login(macAddress);
            Console.WriteLine($"Your device address is: {macAddress}");
        }
        else
        {
            Console.WriteLine("Warning: Could not determine local Bluetooth address.");
            _sessionManager.Login("UnknownUser");
        }
    }

    public async Task StopAsync()
    {
        // Stop the Bluetooth services gracefully before purging data.
        await _bluetoothService.StopAsync();

        // Per PRD: Entire message cache is purged on logout
        await _sessionManager.LogoutAsync();
        Console.WriteLine("Session data purged.");
    }

    public async Task SendMessageAsync(string peerName, string message)
    {
        await _bluetoothService.SendMessageAsync(peerName, message);
        // Also store our own sent message (isDelivered: false, as we just sent it)
        await _storageService.StoreMessageAsync(peerName, $"Me: {message}", isDelivered: false);
    }

    public IReadOnlyDictionary<string, DiscoveredPeer> GetDiscoveredPeers()
    {
        return _bluetoothService.GetDiscoveredPeers();
    }

    public async Task<List<ChatMessage>> GetChatHistoryAsync(string peerName)
    {
        return await _storageService.GetMessagesForChatAsync(peerName);
    }

    public async Task StartChatSessionAsync(string peerName)
    {
        CurrentChatPartner = peerName;
        Console.Clear();
        Console.WriteLine($"--- Chat with {peerName}. Type '/exit' to return to the menu. ---");

        var history = await GetChatHistoryAsync(peerName);
        foreach (var message in history)
        {
            Console.WriteLine($"[{message.Timestamp:HH:mm:ss}] {message.Content}");
        }

        Console.Write($"[{CurrentChatPartner}]> ");
    }

    public void EndChatSession()
    {
        Console.WriteLine($"--- Exiting chat with {CurrentChatPartner}. ---");
        CurrentChatPartner = null;
        Console.Clear();
    }

    private void OnPeerDiscovered(object? sender, BluetoothDeviceInfo deviceInfo)
    {
        // Per PRD: Active users within range presented
        DisplaySystemNotification($"[SYSTEM] Discovered peer: {deviceInfo.DeviceName} ({deviceInfo.DeviceAddress})");
    }

    private async void OnMessageReceived(object? sender, (string peer, string message) data)
    {
        try
        {
            // Format the message with the sender's name for consistent storage.
            var formattedMessage = $"{data.peer}: {data.message}";
            // This is an incoming message, so it has been "delivered" to us.
            await _storageService.StoreMessageAsync(data.peer, formattedMessage, isDelivered: true);

            // Always display the incoming message. The DisplaySystemNotification method
            // is smart enough to handle redrawing the prompt if the user is in a chat.
            DisplaySystemNotification($"[{DateTime.Now:HH:mm:ss}] {formattedMessage}");
        }
        catch (Exception ex)
        {
            DisplaySystemNotification($"[ERROR] Failed to process incoming message: {ex.Message}");
        }
    }

    /// <summary>
    /// Displays a notification to the user, redrawing the input prompt if currently in a chat.
    /// </summary>
    private void DisplaySystemNotification(string notification)
    {
        if (!string.IsNullOrEmpty(CurrentChatPartner))
        {
            // This pattern ensures the notification appears on a new line above the user's current input.
            Console.WriteLine($"\n{notification}");
            Console.Write($"[{CurrentChatPartner}]> "); // Redraw prompt
        }
        else
        {
            Console.WriteLine(notification);
        }
    }
}