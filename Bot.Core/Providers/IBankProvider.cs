// Bot.Core.Providers/IBankProvider.cs
using System;
using System.Threading.Tasks;
using Bot.Shared.Models;

namespace Bot.Core.Providers
{
    public interface IBankProvider
    {
        Task<string> CreateCustomerAsync(User user);
        Task<string> CreateMandateAsync(User user, string customerId, decimal maxAmount, string mandateReference);
        Task<string> InitiateDebitAsync(string mandateId, decimal amount, string reference, string narration);
        Task<decimal> GetBalanceAsync(string accountRef);
    }

}