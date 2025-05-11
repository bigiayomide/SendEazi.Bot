using System.Security.Cryptography;
using System.Text;

namespace Bot.Core.StateMachine.Helpers;

public static class SignatureVerifier
{
    public static bool HmacIsValid(string rawBody, string secret, string incomingSignature)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var payload  = Encoding.UTF8.GetBytes(rawBody);

        using var hmac = new HMACSHA256(keyBytes);
        var computed = hmac.ComputeHash(payload);
        var hex = Convert.ToHexStringLower(computed);

        return string.Equals(hex, incomingSignature, StringComparison.OrdinalIgnoreCase);
    }
}