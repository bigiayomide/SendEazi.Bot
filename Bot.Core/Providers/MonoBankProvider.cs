using System.Net.Http.Json;
using System.Text.Json;
using Bot.Core.Services;
using Bot.Infrastructure.Configuration;
using Bot.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bot.Core.Providers
{

    public class MonoBankProvider : IBankProvider
    {
        private readonly HttpClient _http;
        private readonly MonoOptions _opts;
        private readonly ILogger<MonoBankProvider> _log;
        private readonly IEncryptionService _crypto;

        public MonoBankProvider(HttpClient http, IOptions<MonoOptions> opts, ILogger<MonoBankProvider> log,
            IEncryptionService crypto)
        {
            _http = http;
            _opts = opts.Value;
            _log = log;
            _crypto = crypto;
            _http.BaseAddress = new Uri(_opts.BaseUrl);
            _http.DefaultRequestHeaders.Add("mono-sec-key", _opts.SecretKey);
        }

        public async Task<string> CreateCustomerAsync(User user)
        {
            var payload = new
            {
                first_name = user.FullName.Split(' ')[0],
                last_name = user.FullName.Contains(' ') ? user.FullName[(user.FullName.IndexOf(' ') + 1)..] : "NA",
                phone = user.PhoneNumber,
                type = "individual",
                email = $"{user.Id}@bot.fake",
                bvn = _crypto.Decrypt(user.NINEnc)
            };

            var res = await _http.PostAsJsonAsync("/v2/customers", payload);
            res.EnsureSuccessStatusCode();
            var body = await res.Content.ReadFromJsonAsync<MonoResponse>();
            return body?.Data?.Id ?? throw new Exception("No customer ID returned from Mono");
        }

        public async Task<string> CreateMandateAsync(User user, string customerId, decimal maxAmount, string reference)
        {
            var payload = new
            {
                customer = customerId,
                amount = (int)(maxAmount * 100),
                reference,
                type = "recurring-debit",
                method = "mandate",
                mandate_type = "emandate",
                description = "Auto debit agreement",
                debit_type = "variable",
                start_date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                end_date = DateTime.UtcNow.AddYears(1).ToString("yyyy-MM-dd"),
                split = new
                {
                    type = "flat",
                    sub_account_id = _opts.BusinessSubAccountId,
                    value = 100
                }
            };

            var res = await _http.PostAsJsonAsync("/v2/payments/mandates", payload);
            res.EnsureSuccessStatusCode();
            var body = await res.Content.ReadFromJsonAsync<MonoResponse>();
            return body?.Data?.Id ?? throw new Exception("No mandate ID returned");
        }

        public async Task<string> InitiateDebitAsync(string mandateId, decimal amount, string reference,
            string narration)
        {
            var payload = new
            {
                amount = (int)(amount * 100),
                reference,
                narration
            };

            var res = await _http.PostAsJsonAsync($"/v3/payments/mandates/{mandateId}/debit", payload);
            res.EnsureSuccessStatusCode();
            var body = await res.Content.ReadFromJsonAsync<MonoResponse>();
            return body?.Data?.TransactionId ?? "debit-requested";
        }

        public async Task<decimal> GetBalanceAsync(string accountRef)
        {
            var res = await _http.GetAsync($"/v1/accounts/{accountRef}/balance");
            res.EnsureSuccessStatusCode();
            var json = await res.Content.ReadFromJsonAsync<JsonElement>();
            return json.GetProperty("data").GetProperty("available_balance").GetDecimal();
        }

        
        private record MonoResponse(MonoData? Data);

        private record MonoData(string Id, string? TransactionId);
    }
}
