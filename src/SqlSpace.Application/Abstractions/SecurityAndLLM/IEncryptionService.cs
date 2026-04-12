namespace SqlSpace.Application.Abstractions.Security;

/// <summary>
/// Provides encryption and decryption for sensitive data using ASP.NET Data Protection API.
/// </summary>
/// <remarks>
/// Usage:
/// - Used to encrypt database passwords and connection strings before persistence.
/// - Used to decrypt credentials when opening external database connections.
///
/// When:
/// - Before saving connection credentials to database (encryption).
/// - Before connecting to external database (decryption).
///
/// Why:
/// - Ensures credentials are never stored in plain text.
/// - Leverages ASP.NET Data Protection for key management and rotation.
/// - Provides defense-in-depth against database compromise.
///
/// Where:
/// - Interface consumed by connection management and query execution services.
/// - Implemented in Infrastructure security layer using IDataProtector.
///
/// How:
/// - Use purpose-scoped data protector from Data Protection API.
/// - Apply encryption/decryption with automatic key management.
/// - Handle encrypted payload as Base64 or UTF-8 string.
/// </remarks>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts sensitive plain text data for secure storage.
    /// </summary>
    /// <param name="plainText">Unencrypted sensitive data (password, connection string, etc.).</param>
    /// <returns>Encrypted and Base64-encoded string safe for database storage.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Validate input is not null or empty.
    /// 2. Get purpose-scoped data protector (e.g., "ConnectionCredentials").
    /// 3. Convert plain text to byte array.
    /// 4. Apply data protector encryption.
    /// 5. Encode encrypted bytes to Base64 string.
    /// 6. Return encrypted payload.
    /// </remarks>
    string Encrypt(string plainText);

    /// <summary>
    /// Decrypts previously encrypted data for runtime use.
    /// </summary>
    /// <param name="encryptedText">Base64-encoded encrypted string from database.</param>
    /// <returns>Decrypted plain text original value.</returns>
    /// <exception cref="System.Security.Cryptography.CryptographicException">
    /// Thrown when decryption fails (corrupted data, wrong key, tampering).
    /// </exception>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Validate input is not null or empty.
    /// 2. Decode Base64 string to encrypted byte array.
    /// 3. Get purpose-scoped data protector (same purpose as encryption).
    /// 4. Apply data protector decryption.
    /// 5. Convert decrypted bytes back to string.
    /// 6. Return plain text value.
    /// 7. Throw exception if decryption fails (key rotation, corruption, tampering).
    /// </remarks>
    string Decrypt(string encryptedText);
}
