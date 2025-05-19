using System.Security.Cryptography;
using System.Text;

namespace Bot.Core.Helpers;

public static class SignatureVerifier
{
    private static HMACSHA256 CreateHmac(string secret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        return new HMACSHA256(keyBytes);
    }

    private static string ComputeSignature(HMACSHA256 hmac, string payload)
    {
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var hash = hmac.ComputeHash(payloadBytes);
        return Convert.ToHexString(hash).ToLower();
    }

    public static bool HmacIsValid(string rawBody, string secret, string incomingSignature)
    {
        using var hmac = CreateHmac(secret);
        var computedSignature = ComputeSignature(hmac, rawBody);
        return string.Equals(computedSignature, incomingSignature, StringComparison.OrdinalIgnoreCase);
    }

    public static bool MetaSecretVerifier(string headerValue, string rawBody, string secret)
    {
        if (!headerValue.StartsWith("sha256="))
            return false;

        using var hmac = CreateHmac(secret);
        var computedSignature = ComputeSignature(hmac, rawBody);
        var signature = headerValue.AsSpan(7).ToString();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedSignature),
            Encoding.UTF8.GetBytes(signature));
    }
}