using InTheHand.Net.Sockets;

namespace BTChat;

/// <summary>
/// Handles the user interface logic for peer selection and chat sessions.
/// </summary>
public static class ChatUI
{
    /// <summary>
    /// Displays discovered peers and prompts the user to select one for a chat.
    /// </summary>
    /// <returns>The selected peer's device info, or null if the user wants to exit.</returns>
    public static BluetoothDeviceInfo? SelectPeer(ChatClient client)
    {
        Console.WriteLine("\nScanning for nearby devices... Discovered devices will appear below.");
        var peers = client.GetDiscoveredPeers().Values.ToList();

        if (!peers.Any())
        {
            Console.WriteLine("No devices found yet. Press Enter to refresh or type 'exit' to quit.");
            return null;
        }

        Console.WriteLine("--- Nearby Devices ---");
        for (int i = 0; i < peers.Count; i++)
        {
            var peer = peers[i];
            var peerInfo = peer.Info;
            var status = new List<string>();

            if (peer.IsChatPeer)
            {
                status.Add("BTChat User");
            }
            if (peerInfo.Authenticated)
            {
                status.Add("Paired");
            }

            var statusText = status.Any() ? $"[{string.Join(", ", status)}]" : "";
            // Pad the device name for better alignment of the status text
            Console.WriteLine($"{i + 1}. {peerInfo.DeviceName,-25} {statusText}");
        }
        Console.WriteLine("--------------------");
        Console.Write("Enter a number to chat (BTChat Users only), press Enter to refresh, or type 'exit': ");

        var input = Console.ReadLine();
        if (int.TryParse(input, out var selection) && selection > 0 && selection <= peers.Count)
        {
            var selectedPeer = peers[selection - 1];
            if (!selectedPeer.IsChatPeer)
            {
                Console.WriteLine("\nError: You can only start a chat with a 'BTChat User'. Press Enter to continue.");
                Console.ReadLine();
                return null; // This will cause a refresh of the list
            }
            return selectedPeer.Info; // Return the underlying BluetoothDeviceInfo
        }

        if (input?.ToLower() == "exit")
        {
            // A special value to signal the main loop to exit
            return new BluetoothDeviceInfo(new InTheHand.Net.BluetoothAddress(0UL));
        }

        return null; // User wants to refresh
    }

    /// <summary>
    /// Enters a dedicated chat mode with a specific peer.
    /// </summary>
    public static async Task StartChatSessionAsync(ChatClient client, BluetoothDeviceInfo peer)
    {
        Console.Clear();
        Console.WriteLine($"--- Chatting with {peer.DeviceName} ---");
        Console.WriteLine("Type '/exit' to return to the user list.");
        Console.WriteLine("---------------------------------------");

        // Show recent chat history upon entering
        var messages = await client.GetChatHistoryAsync(peer.DeviceName);
        foreach (var msg in messages)
        {
            Console.WriteLine($"[{msg.Timestamp:HH:mm:ss}] {msg.Content}");
        }

        while (true)
        {
            Console.Write($"[{peer.DeviceName}]> ");
            var message = Console.ReadLine();

            if (message?.ToLower() == "/exit")
            {
                client.EndChatSession(); // This correctly resets the chat state
                return;
            }

            if (!string.IsNullOrWhiteSpace(message))
            {
                await client.SendMessageAsync(peer.DeviceName, message);
                // Echo the sent message to the user's own screen for context.
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Me: {message}");
            }
        }
    }
}