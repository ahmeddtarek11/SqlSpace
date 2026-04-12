using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SqlSpace.Application.Abstractions.Security;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using System.Text;

namespace SqlSpace.Infrastructure.Security;

public class EncryptionService(IDataProtectionProvider protectionProvider) : IEncryptionService
{
    private readonly IDataProtector _protector = protectionProvider.CreateProtector("ConnectionCredentials");

    public string Decrypt(string encryptedText)
    {
        if(string.IsNullOrEmpty(encryptedText))
        {
            throw new ArgumentException("Encrypted Text Cannot Be null or empty" ,nameof(encryptedText));
        }

        byte[] EncryptedBytes =Convert.FromBase64String(encryptedText);
        byte[] DecryptedBytes = _protector.Unprotect(EncryptedBytes);

        return Encoding.UTF8.GetString(DecryptedBytes);
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            throw new ArgumentException("Can't Encrypt a null or empty string" , nameof(plainText));
        }
        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
        byte[] EncryptedBytes = _protector.Protect(plainBytes);

        return Convert.ToBase64String(EncryptedBytes);




    }
}
