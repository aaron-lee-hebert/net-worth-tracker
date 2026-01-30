namespace NetWorthTracker.Core.Interfaces;

/// <summary>
/// Service for encrypting and decrypting sensitive data.
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts a plaintext string.
    /// </summary>
    /// <param name="plainText">The text to encrypt.</param>
    /// <returns>The encrypted ciphertext, or null if input is null.</returns>
    string? Encrypt(string? plainText);

    /// <summary>
    /// Decrypts a ciphertext string.
    /// </summary>
    /// <param name="cipherText">The encrypted text to decrypt.</param>
    /// <returns>The decrypted plaintext, or null if input is null.</returns>
    string? Decrypt(string? cipherText);

    /// <summary>
    /// Checks if a value appears to be encrypted.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns>True if the value appears to be encrypted, false otherwise.</returns>
    bool IsEncrypted(string? value);
}
