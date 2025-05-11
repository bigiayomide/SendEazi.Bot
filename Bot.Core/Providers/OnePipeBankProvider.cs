using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Bot.Core.Models;
using Bot.Core.Services;
using Bot.Infrastructure.Configuration;
using Bot.Shared.Models;
using Microsoft.Extensions.Options;

namespace Bot.Core.Providers;

public class OnePipeBankProvider : IBankProvider
{
    private readonly IEncryptionService _encryption;
    private readonly HttpClient _http;
    private readonly OnePipeOptions _opts;

    public OnePipeBankProvider(HttpClient http, IOptions<OnePipeOptions> opts, IEncryptionService encryption)
    {
        _http = http;
        _encryption = encryption;
        _opts = opts.Value;
        _http.BaseAddress = new Uri(_opts.BaseUrl);
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {_opts.ApiKey}");
    }

    public Task<string> CreateCustomerAsync(User user)
    {
        return Task.FromResult("na");
    }

    public async Task<string> CreateMandateAsync(User user, string customerId, decimal maxAmount, string reference)
    {
        var payload = new
        {
            request_ref = reference,
            request_type = "pay_with_account",
            auth = new
            {
                type = "bank.account",
                secure = Encrypt(_encryption.Decrypt(user.BVNEnc)),
                auth_provider = "PayWithAccount"
            },
            transaction = new
            {
                transaction_ref = reference,
                transaction_desc = "Debit agreement",
                amount = (int)(maxAmount * 100),
                customer = new
                {
                    name = user.FullName,
                    mobile_no = "234" + user.PhoneNumber.TrimStart('+'),
                    bvn = Encrypt(_encryption.Decrypt(user.BVNEnc))
                }
            }
        };

        var res = await _http.PostAsJsonAsync("/transact", payload);
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<OnePipeResp>();
        return body?.Data.TransactionRef ?? "mandate-started";
    }

    public async Task<string> InitiateDebitAsync(string mandateId, decimal amount, string reference,
        string narration)
    {
        var payload = new
        {
            request_ref = reference,
            request_type = "debit",
            auth = new
            {
                type = "bank.account",
                secure = Encrypt(mandateId),
                auth_provider = "PayWithAccount"
            },
            transaction = new
            {
                transaction_ref = reference,
                transaction_desc = narration,
                amount = (int)(amount * 100),
                customer = new { name = "N/A", mobile_no = "N/A" }
            }
        };

        var res = await _http.PostAsJsonAsync("/transact", payload);
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<OnePipeResp>();
        return body?.Data.TransactionRef ?? "debit-requested";
    }

    public async Task<decimal> GetBalanceAsync(string accountRef)
    {
        var payload = new { account_ref = accountRef };
        var res = await _http.PostAsJsonAsync("/v1/balance", payload);
        res.EnsureSuccessStatusCode();
        var json = await res.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("data").GetProperty("available_balance").GetDecimal();
    }

    public Task<AccountDetails> GetAccountDetailsAsync(string mandateId)
    {
        throw new NotImplementedException();
    }


    private string Encrypt(string bvn)
    {
        var key = Encoding.UTF8.GetBytes(_opts.SecretKey[..24]);
        using var tdes = TripleDES.Create();
        tdes.Key = key;
        tdes.Mode = CipherMode.ECB;
        tdes.Padding = PaddingMode.PKCS7;

        var encryptor = tdes.CreateEncryptor();
        var input = Encoding.UTF8.GetBytes(bvn);
        var encrypted = encryptor.TransformFinalBlock(input, 0, input.Length);
        return Convert.ToBase64String(encrypted);
    }

    private record OnePipeResp(OnePipeData Data);

    private record OnePipeData(string TransactionRef);
}