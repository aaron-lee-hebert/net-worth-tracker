using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using NetWorthTracker.Core.Interfaces;

namespace NetWorthTracker.Infrastructure.Services;

public class EncryptionService : IEncryptionService
{
    private readonly IDataProtector _protector;
    private readonly ILogger<EncryptionService> _logger;

    // Prefix to identify encrypted values
    private const string EncryptedPrefix = "ENC:";

    // Purpose string for key rotation support
    private const string Purpose = "NetWorthTracker.AccountNumber.v1";

    public EncryptionService(IDataProtectionProvider provider, ILogger<EncryptionService> logger)
    {
        _protector = provider.CreateProtector(Purpose);
        _logger = logger;
    }

    public string? Encrypt(string? plainText)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            return plainText;
        }

        try
        {
            var encrypted = _protector.Protect(plainText);
            return $"{EncryptedPrefix}{encrypted}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt value");
            throw;
        }
    }

    public string? Decrypt(string? cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
        {
            return cipherText;
        }

        // If not encrypted, return as-is (supports migration of existing data)
        if (!IsEncrypted(cipherText))
        {
            return cipherText;
        }

        try
        {
            var encryptedPart = cipherText.Substring(EncryptedPrefix.Length);
            return _protector.Unprotect(encryptedPart);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt value");
            throw;
        }
    }

    public bool IsEncrypted(string? value)
    {
        return !string.IsNullOrEmpty(value) && value.StartsWith(EncryptedPrefix);
    }
}
