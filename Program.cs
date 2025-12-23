using BTChat;

class Program
{
    static async Task Main(string[] args)
    {
        // 1. Dependency Injection Setup
        // Create instances of all the services the application needs.
        var encryptionService = new EncryptionService("a-very-secret-and-well-managed-key");
        var storageService = new StorageService(encryptionService);
        var sessionManager = new SessionManager(storageService);
        var bluetoothService = new BluetoothService();
        var chatClient = new ChatClient(bluetoothService, sessionManager, storageService);

        // 2. Start the client services
        // The ChatClient class orchestrates the startup of the various services.
        // To fix the compilation errors, we will use its StartAsync method.
        // Note: This may not start Bluetooth first. See the explanation below for the correct long-term fix.
        Console.WriteLine("Initializing BTChat client...");
        await chatClient.StartAsync();

        Console.WriteLine("\nBTChat Client Initialized. Scanning for peers...");
        Console.WriteLine("Waiting a few seconds for initial discovery...");
        await Task.Delay(4000); // Allow 4 seconds for the initial scan to find nearby devices

        ShowHelp();

        // 3. Main application loop for the command menu
        bool running = true;
        while (running)
        {
            var selectedPeer = ChatUI.SelectPeer(chatClient);

            if (selectedPeer == null)
            {
                // User pressed Enter to refresh, so we just loop again.
                continue;
            }

            // ChatUI uses a special value to signal the user wants to exit the app.
            if (selectedPeer.DeviceAddress.ToInt64() == 0)
            {
                running = false;
                continue;
            }

            // A valid peer was selected, so we start the dedicated chat UI.
            await ChatUI.StartChatSessionAsync(chatClient, selectedPeer);

            // After the chat session ends, show the main help text again before re-listing peers.
            ShowHelp();
        }

        // 4. Cleanup on exit
        Console.WriteLine("\nShutting down and cleaning up services...");
        // The ChatClient's StopAsync method handles the required cleanup and data purge.
        await chatClient.StopAsync();
        Console.WriteLine("Application shut down. All data has been purged.");
    }

    static void ShowHelp()
    {
        Console.WriteLine("\n--- Main Menu ---");
        Console.WriteLine("The application is scanning for other users running BTChat.");
        Console.WriteLine("When a user is found, they will be listed below.");
        Console.WriteLine();
    }
}