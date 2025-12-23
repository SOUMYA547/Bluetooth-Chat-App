using System.Security.Cryptography;

namespace BTChat;

/// <summary>
/// Provides strong password hashing and verification using PBKDF2.
/// </summary>
public static class PasswordHelper
{
    // These constants can be tuned for security vs. performance
    private const int SaltSize = 16; // 128 bit
    private const int HashSize = 20; // 160 bit
    private const int Iterations = 10000;

    /// <summary>
    /// Creates a salted PBKDF2 hash of the password.
    /// </summary>
    /// <param name="password">The password to hash.</param>
    /// <returns>The hash as a base64-encoded string.</returns>
    public static string HashPassword(string password)
    {
        // 1. Create a random salt
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

        // 2. Create the hash
        var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA1);
        var hash = pbkdf2.GetBytes(HashSize);

        // 3. Combine salt and hash: [salt][hash]
        var hashBytes = new byte[SaltSize + HashSize];
        Array.Copy(salt, 0, hashBytes, 0, SaltSize);
        Array.Copy(hash, 0, hashBytes, SaltSize, HashSize);

        // 4. Convert to base64 for storage
        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// Verifies a password against a stored hash.
    /// </summary>
    public static bool VerifyPassword(string password, string hashedPassword)
    {
        var hashBytes = Convert.FromBase64String(hashedPassword);
        var salt = new byte[SaltSize];
        Array.Copy(hashBytes, 0, salt, 0, SaltSize);

        var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA1);
        byte[] hash = pbkdf2.GetBytes(HashSize);

        // This is a constant-time comparison to prevent timing attacks.
        // It compares all bytes of the hash, ensuring the operation takes
        // the same amount of time regardless of where the first difference occurs.
        uint diff = (uint)hashBytes.Length ^ (uint)(SaltSize + HashSize);
        for (int i = 0; i < HashSize; i++)
        {
            diff |= (uint)(hashBytes[i + SaltSize] ^ hash[i]);
        }

        return diff == 0;
    }
}