// Bot.Core.Services/ReferenceGenerator.cs

using System.Security.Cryptography;
using System.Text;

namespace Bot.Core.Services;

public interface IReferenceGenerator
{
    string GenerateTransferRef(Guid userId, string toAccount, string bankCode);
    string GenerateRecurringRef(Guid recurringId);
    string GenerateMandateRef(Guid userId);
}

public class ReferenceGenerator : IReferenceGenerator
{
    public string GenerateTransferRef(Guid userId, string toAccount, string bankCode)
    {
        var input = $"{userId}:{toAccount}:{bankCode}:{DateTime.UtcNow:yyyyMMddHHmmss}";
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        var shortHash = Convert.ToHexString(hash)[..12].ToLower();
        return $"txn:{userId}:{shortHash}";
    }

    public string GenerateRecurringRef(Guid recurringId)
    {
        return $"rec:{recurringId.ToString("N")[..12]}";
    }

    public string GenerateMandateRef(Guid userId)
    {
        return $"mandate:{userId}";
    }
}