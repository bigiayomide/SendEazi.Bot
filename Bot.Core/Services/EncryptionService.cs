using System.Security.Cryptography;
using System.Text;
using Bot.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace Bot.Core.Services;

public interface IEncryptionService
{
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
    string Sha256(string input);
}

public class EncryptionService(IOptions<AppSettings> settings) : IEncryptionService
{
    private readonly byte[] _key = Convert.FromBase64String(settings.Value.EncryptionBase64Key);

    public string Encrypt(string plaintext)
    {
        using var aes = new AesGcm(_key);
        var iv = RandomNumberGenerator.GetBytes(12);
        var pt = Encoding.UTF8.GetBytes(plaintext);
        var ct = new byte[pt.Length];
        var tag = new byte[16];
        aes.Encrypt(iv, pt, ct, tag);
        return Convert.ToBase64String(iv) + "|" +
               Convert.ToBase64String(tag) + "|" +
               Convert.ToBase64String(ct);
    }

    public string Decrypt(string cipher)
    {
        var parts = cipher.Split('|');
        var iv = Convert.FromBase64String(parts[0]);
        var tag = Convert.FromBase64String(parts[1]);
        var ct = Convert.FromBase64String(parts[2]);
        var pt = new byte[ct.Length];
        using var aes = new AesGcm(_key);
        aes.Decrypt(iv, ct, tag, pt);
        return Encoding.UTF8.GetString(pt);
    }

    public string Sha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}