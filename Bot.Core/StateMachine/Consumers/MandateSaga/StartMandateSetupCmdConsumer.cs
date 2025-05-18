using Bot.Core.Providers;
using Bot.Core.Services;
using Bot.Infrastructure.Data;
using Bot.Shared.DTOs;
using Bot.Shared.Models;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bot.Core.StateMachine.Consumers.MandateSaga;

public class StartMandateSetupCmdConsumer(
    IUserService users,
    IBankProviderFactory factory,
    IEncryptionService encryption,
    ApplicationDbContext db,
    ILogger<StartMandateSetupCmdConsumer> log)
    : IConsumer<StartMandateSetupCmd>
{
    public async Task Consume(ConsumeContext<StartMandateSetupCmd> ctx)
    {
        var user = await users.GetByIdAsync(ctx.Message.CorrelationId);
        if (user is null)
        {
            log.LogWarning("User not found for mandate setup: {Cid}", ctx.Message.CorrelationId);
            return;
        }

        var provider = await factory.GetProviderAsync(user.Id);
        var customerId = await provider.CreateCustomerAsync(user);

        var mandateId = await provider.CreateMandateAsync(
            user,
            customerId,
            ctx.Message.MaxAmount,
            $"mandate:{user.Id}");

        // Retrieve account details from Mono
        var accountDetails = await provider.GetAccountDetailsAsync(mandateId);

        var encryptedAccountNumber = encryption.Encrypt(accountDetails.AccountNumber);
        var accountHash = encryption.Sha256(accountDetails.AccountNumber);
        var bankCode = accountDetails.BankCode;

        var defaultAlreadySet = await db.LinkedBankAccounts
            .AnyAsync(x => x.UserId == user.Id && x.IsDefault);

        var alreadyLinked = await db.LinkedBankAccounts.AnyAsync(x =>
            x.UserId == user.Id &&
            x.AccountHash == accountHash &&
            x.BankCode == bankCode);

        if (!alreadyLinked)
        {
            db.LinkedBankAccounts.Add(new LinkedBankAccount
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Provider = "Mono",
                AccountName = accountDetails.AccountName,
                AccountNumberEnc = encryptedAccountNumber,
                AccountHash = accountHash,
                BankCode = bankCode,
                ProviderCustomerId = customerId,
                IsDefault = !defaultAlreadySet
            });

            await db.SaveChangesAsync();
        }

        log.LogInformation("Mandate created and account linked for user {UserId}", user.Id);
    }
}