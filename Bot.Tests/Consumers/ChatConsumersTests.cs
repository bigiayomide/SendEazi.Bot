using Bot.Core.Services;
using Bot.Core.Models;
using Bot.Core.StateMachine.Consumers.Chat;
using Bot.Infrastructure.Data;
using Bot.Shared;
using Bot.Shared.DTOs;
using Bot.Shared.Enums;
using Bot.Shared.Models;
using Bot.Tests.TestUtilities;
using FluentAssertions;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Bot.Tests.Consumers;

public class ChatConsumersTests
{
    [Fact]
    public async Task BankLinkCmd_Should_Publish_StartMandate()
    {
        var userId = Guid.NewGuid();
        var enc = new Mock<IEncryptionService>();
        enc.Setup(e => e.Decrypt("bvn")).Returns("12345678901");
        var userSvc = new Mock<IUserService>();
        userSvc.Setup(u => u.GetByIdAsync(userId)).ReturnsAsync(new User
            { Id = userId, PhoneNumber = "234", FullName = "Joe", BVNEnc = "bvn" });

        var harness = await TestContextHelper.BuildTestHarness<BankLinkCmdConsumer>(services =>
        {
            services.AddSingleton(userSvc.Object);
            services.AddSingleton(enc.Object);
        });

        await harness.Bus.Publish(new BankLinkCmd(userId));

        (await harness.Published.Any<StartMandateSetupCmd>()).Should().BeTrue();
        await harness.Stop();
    }

    [Fact]
    public async Task KycCmd_Should_Publish_Approved_When_Success()
    {
        var userId = Guid.NewGuid();
        var svc = new Mock<IUserService>();
        svc.Setup(s => s.RunKycAsync(userId)).ReturnsAsync(true);

        var harness = await TestContextHelper.BuildTestHarness<KycCmdConsumer>(services =>
        {
            services.AddSingleton(svc.Object);
        });

        await harness.Bus.Publish(new KycCmd(userId));

        (await harness.Published.Any<KycApproved>()).Should().BeTrue();
        await harness.Stop();
    }

    [Fact]
    public async Task NlpFromText_Should_Publish_Detected()
    {
        var userId = Guid.NewGuid();
        var nlp = new Mock<INlpService>();
        nlp.Setup(n => n.DetectIntentAsync(userId, "hi", "+234"))
            .ReturnsAsync(new UserIntentDetected(userId, IntentType.Greeting));

        var harness = await TestContextHelper.BuildTestHarness<NlpFromTextConsumer>(services =>
        {
            services.AddSingleton(nlp.Object);
        });

        await harness.Bus.Publish(new VoiceMessageTranscribed(userId, "hi", "en", "+234"));

        (await harness.Published.Any<UserIntentDetected>()).Should().BeTrue();
        await harness.Stop();
    }

    [Fact]
    public async Task PinSetupCmd_Should_Publish_PinSet()
    {
        var userId = Guid.NewGuid();
        var pinSvc = new Mock<IPinService>();
        var wa = new Mock<IWhatsAppService>();
        var userSvc = new Mock<IUserService>();
        userSvc.Setup(u => u.GetByIdAsync(userId)).ReturnsAsync(new User { Id = userId, PhoneNumber = "234" });

        var harness = await TestContextHelper.BuildTestHarness<PinSetupCmdConsumer>(services =>
        {
            services.AddSingleton(pinSvc.Object);
            services.AddSingleton(wa.Object);
            services.AddSingleton(userSvc.Object);
        });

        await harness.Bus.Publish(new PinSetupCmd(userId, "1234", "msgId"));

        (await harness.Published.Any<PinSet>()).Should().BeTrue();
        wa.Verify(w => w.DeleteMessageAsync("msgId"), Times.Once);
        await harness.Stop();
    }

    [Fact]
    public async Task PinValidationCmd_Should_Publish_Validated_When_Ok()
    {
        var userId = Guid.NewGuid();
        var pinSvc = new Mock<IPinService>();
        pinSvc.Setup(p => p.ValidateAsync(userId, "1111")).ReturnsAsync(true);

        var harness = await TestContextHelper.BuildTestHarness<PinValidationCmdConsumer>(services =>
        {
            services.AddSingleton(pinSvc.Object);
        });

        await harness.Bus.Publish(new PinValidationCmd(userId, "1111"));

        (await harness.Published.Any<PinValidated>()).Should().BeTrue();
        await harness.Stop();
    }

    [Fact]
    public async Task RawInboundMsg_Should_Publish_FullNameProvided()
    {
        var sessionSvc = new Mock<IConversationStateService>();
        var session = new ConversationSession
            { SessionId = Guid.NewGuid(), UserId = Guid.NewGuid(), PhoneNumber = "+234" };
        sessionSvc.Setup(s => s.GetOrCreateSessionAsync("+234"))
            .ReturnsAsync(session);
        sessionSvc.Setup(s => s.GetStateAsync(session.SessionId)).ReturnsAsync("AskFullName");

        var harness = await TestContextHelper.BuildTestHarness<RawInboundMsgCmdConsumer>(services =>
        {
            services.AddSingleton(sessionSvc.Object);
            services.AddSingleton(new Mock<ILogger<RawInboundMsgCmdConsumer>>().Object);
        });

        await harness.Bus.Publish(new RawInboundMsgCmd(Guid.NewGuid(), "+234", "John", "msg"));

        (await harness.Published.Any<FullNameProvided>()).Should().BeTrue();
        await harness.Stop();
    }

    [Fact]
    public async Task SetDefaultBankAccountCmd_Should_Update_Accounts()
    {
        var userId = Guid.NewGuid();
        var acc1 = Guid.NewGuid();
        var acc2 = Guid.NewGuid();

        var harness = await TestContextHelper.BuildTestHarness<SetDefaultBankAccountCmdConsumer>();

        using var scope = harness.Scope.ServiceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Seed data using the same DbContext the consumer will use
        await db.LinkedBankAccounts.AddAsync(new LinkedBankAccount
        {
            Id = acc1,
            UserId = userId,
            IsDefault = false,
            AccountHash = "hash1",
            AccountName = "User One",
            BankCode = "001",
            Provider = "MockProvider",
            AccountNumberEnc = "enc1"
        });

        await db.LinkedBankAccounts.AddAsync(new LinkedBankAccount
        {
            Id = acc2,
            UserId = userId,
            IsDefault = true,
            AccountHash = "hash2",
            AccountName = "User Two",
            BankCode = "001",
            Provider = "MockProvider",
            AccountNumberEnc = "enc2"
        });

        await db.SaveChangesAsync();

        await harness.Bus.Publish(new SetDefaultBankAccountCmd(userId, acc1));

        // ✅ Wait until the consumer has actually processed the message
        Assert.True(await harness.Consumed.Any<SetDefaultBankAccountCmd>(), "Message was not consumed");

        // Use a fresh scope to requery from DB
        using var checkScope = harness.Scope.ServiceProvider.CreateScope();
        var checkDb = checkScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        (await checkDb.LinkedBankAccounts.FindAsync(acc1))!.IsDefault.Should().BeTrue();
        (await checkDb.LinkedBankAccounts.FindAsync(acc2))!.IsDefault.Should().BeFalse();

        await harness.Stop();
    }


    [Fact]
    public async Task SignupCmd_Should_Publish_Success()
    {
        var svc = new Mock<IUserService>();
        svc.Setup(s => s.CreateAsync(It.IsAny<Guid>(), It.IsAny<SignupPayload>()))
            .ReturnsAsync(new User { Id = Guid.NewGuid() });

        var harness = await TestContextHelper.BuildTestHarness<SignupCmdConsumer>(services =>
        {
            services.AddSingleton(svc.Object);
        });

        await harness.Bus.Publish(new SignupCmd(Guid.NewGuid(), new SignupPayload("a", "b", "c", "d")));

        (await harness.Published.Any<SignupSucceeded>()).Should().BeTrue();
        await harness.Stop();
    }

    [Fact]
    public async Task ValidateBvnCmd_Should_Publish_Verified()
    {
        var svc = new Mock<IIdentityVerificationService>();
        svc.Setup(s => s.VerifyBvnAsync("1")).ReturnsAsync(true);

        var harness = await TestContextHelper.BuildTestHarness<ValidateBvnCmdConsumer>(services =>
        {
            services.AddSingleton(svc.Object);
        });

        await harness.Bus.Publish(new ValidateBvnCmd(Guid.NewGuid(), "1"));

        (await harness.Published.Any<BvnVerified>()).Should().BeTrue();
        await harness.Stop();
    }

    [Fact]
    public async Task ValidateNinCmd_Should_Publish_Verified()
    {
        var svc = new Mock<IIdentityVerificationService>();
        svc.Setup(s => s.VerifyNinAsync("1")).ReturnsAsync(true);

        var harness = await TestContextHelper.BuildTestHarness<ValidateNinCmdConsumer>(services =>
        {
            services.AddSingleton(svc.Object);
        });

        await harness.Bus.Publish(new ValidateNinCmd(Guid.NewGuid(), "1"));

        (await harness.Published.Any<NinVerified>()).Should().BeTrue();
        await harness.Stop();
    }
}