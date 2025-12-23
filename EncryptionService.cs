using System.Text;

namespace BTChat;

/// <summary>
/// A placeholder encryption service for demonstration purposes.
/// WARNING: This uses simple Base64 encoding and is NOT secure.
/// Replace with a real cryptographic implementation for a production application.
/// </summary>
public class EncryptionService
{
    // In a real app, this key would be securely managed and used for actual encryption.
    public EncryptionService(string key)
    {
        // The key is unused in this placeholder, but the constructor signature matches the dependency.
    }

    public string Encrypt(string plainText) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText));

    public string Decrypt(string cipherText)
    {
        var base64EncodedBytes = Convert.FromBase64String(cipherText);
        return Encoding.UTF8.GetString(base64EncodedBytes);
    }
}