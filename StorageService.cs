using Microsoft.Data.Sqlite;
using System.Diagnostics;

namespace BTChat;

public record ChatMessage(string Content, DateTime Timestamp);

/// <summary>
/// Manages local, encrypted message storage using SQLite.
/// </summary>
public class StorageService
{
    private const string DbName = "btchat_local.db";
    private readonly EncryptionService _encryptionService;

    public StorageService(EncryptionService encryptionService)
    {
        _encryptionService = encryptionService;
    }

    public async Task InitializeDatabaseAsync()
    {
        try
        {
            using var connection = await GetOpenConnectionAsync();
            var command = connection.CreateCommand();
            // Per PRD: Locally store only the last 15–16 messages per chat.
            // Data is encrypted on disk.
            command.CommandText =
            @"
                CREATE TABLE IF NOT EXISTS Messages (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ChatSessionId TEXT NOT NULL,
                    EncryptedContent TEXT NOT NULL,
                    Timestamp DATETIME NOT NULL,
                    IsDelivered INTEGER NOT NULL DEFAULT 0
                );
            ";
            await command.ExecuteNonQueryAsync();
        }
        catch (SqliteException ex)
        {
            // Per PRD: Backend detects and surfaces... database (SQLite) issues.
            Console.WriteLine($"Database initialization failed: {ex.Message}");
            // In a real UI app, you would surface this error to the user.
            throw; // Re-throwing allows the calling service to know initialization failed.
        }
    }

    /// <summary>
    /// Per PRD: Stores a message and prunes the log to keep only the last 16 messages for that session.
    /// </summary>
    public async Task StoreMessageAsync(string chatSessionId, string messageContent, bool isDelivered)
    {
        // Per PRD: Message encryption and decryption occurs in C# backend.
        var encryptedMessage = _encryptionService.Encrypt(messageContent);

        try
        {
            using var connection = await GetOpenConnectionAsync();
            using var transaction = await connection.BeginTransactionAsync();

            // 1. Insert the new message
            var insertCommand = connection.CreateCommand();
            insertCommand.CommandText =
            @"
                INSERT INTO Messages (ChatSessionId, EncryptedContent, Timestamp, IsDelivered)
                VALUES ($sessionId, $content, datetime('now'), $isDelivered)
            ";
            insertCommand.Parameters.AddWithValue("$sessionId", chatSessionId);
            insertCommand.Parameters.AddWithValue("$content", encryptedMessage);
            insertCommand.Parameters.AddWithValue("$isDelivered", isDelivered ? 1 : 0);
            await insertCommand.ExecuteNonQueryAsync();

            // 2. Prune old messages, keeping only the most recent 16
            var pruneCommand = connection.CreateCommand();
            pruneCommand.CommandText =
            @"
                DELETE FROM Messages
                WHERE Id IN (
                    SELECT Id FROM Messages
                    WHERE ChatSessionId = $sessionId
                    ORDER BY Timestamp DESC
                    LIMIT -1 OFFSET 16
                )
            ";
            pruneCommand.Parameters.AddWithValue("$sessionId", chatSessionId);
            await pruneCommand.ExecuteNonQueryAsync();

            await transaction.CommitAsync();
        }
        catch (SqliteException ex)
        {
            Console.WriteLine($"Failed to store message: {ex.Message}");
            // Decide how to handle this. Maybe retry or notify the user.
        }
    }

    /// <summary>
    /// Per PRD: Entire message cache is purged on logout, session close, or expiry.
    /// </summary>
    public async Task PurgeAllDataAsync()
    {
        await Task.Run(() =>
        {
            if (File.Exists(DbName))
            {
                // Ensure all pooled connections are released before deleting the file.
                SqliteConnection.ClearAllPools();
                File.Delete(DbName);
            }
        });
    }

    /// <summary>
    /// Retrieves the stored messages for a specific chat session.
    /// </summary>
    /// <returns>A list of decrypted chat messages, ordered by time.</returns>
    public async Task<List<ChatMessage>> GetMessagesForChatAsync(string chatSessionId)
    {
        var messages = new List<ChatMessage>();
        try
        {
            using var connection = await GetOpenConnectionAsync();
            var command = connection.CreateCommand();
            command.CommandText =
            @"
                SELECT EncryptedContent, Timestamp FROM Messages
                WHERE ChatSessionId = $sessionId
                ORDER BY Timestamp ASC
            ";
            command.Parameters.AddWithValue("$sessionId", chatSessionId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var decryptedContent = _encryptionService.Decrypt(reader.GetString(0));
                messages.Add(new ChatMessage(decryptedContent, reader.GetDateTime(1)));
            }
        }
        catch (SqliteException ex)
        {
            Console.WriteLine($"Failed to retrieve messages: {ex.Message}");
        }
        return messages;
    }

    /// <summary>
    /// Per PRD: Deletes all records older than 20 hours.
    /// </summary>
    public async Task PurgeAllExpiredDataAsync()
    {
        try
        {
            using var connection = await GetOpenConnectionAsync();
            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Messages WHERE Timestamp < datetime('now', '-20 hours')";
            var rowsAffected = await command.ExecuteNonQueryAsync();
            if (rowsAffected > 0)
            {
                Console.WriteLine($"Purged {rowsAffected} expired messages.");
            }
        }
        catch (SqliteException ex)
        {
            Console.WriteLine($"Failed to purge expired data: {ex.Message}");
        }
    }

    /// <summary>
    /// Centralized method to create and open a database connection.
    /// </summary>
    private async Task<SqliteConnection> GetOpenConnectionAsync()
    {
        var connection = new SqliteConnection($"Data Source={DbName}");
        await connection.OpenAsync();
        return connection;
    }
}