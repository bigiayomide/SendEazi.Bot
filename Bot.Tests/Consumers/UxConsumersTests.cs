using System.Net;
using Bot.Core.Services;
using Bot.Core.StateMachine.Consumers.UX;
using Bot.Infrastructure.Data;
using Bot.Shared;
using Bot.Shared.DTOs;
using Bot.Shared.Models;
using Bot.Tests.TestUtilities;
using FluentAssertions;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Bot.Tests.Consumers;

public class UxConsumersTests
{
    [Fact]
    public async Task QuickReplyCmd_Should_Send_And_Publish()
    {
        var userId = Guid.NewGuid();

        var wa = new Mock<IWhatsAppService>();
        var replySvc = new Mock<IQuickReplyService>();
        replySvc.Setup(r => r.GetQuickRepliesAsync(userId, 5)).ReturnsAsync(["A", "B"]);

        var userSvc = new Mock<IUserService>();
        userSvc.Setup(u => u.GetByIdAsync(userId)).ReturnsAsync(new User
        {
            Id = userId,
            PhoneNumber = "234" // ✅ Match exactly what consumer will use
        });

        var harness = await TestContextHelper.BuildTestHarness<QuickReplyCmdConsumer>(services =>
        {
            services.AddSingleton(wa.Object);
            services.AddSingleton(replySvc.Object);
            services.AddSingleton(userSvc.Object);
        });

        await harness.Bus.Publish(new QuickReplyCmd(userId, "tmpl", new[] { "Hello" }));

        // ✅ Ensure consumer executed
        Assert.True(await harness.Consumed.Any<QuickReplyCmd>(), "QuickReplyCmd was not consumed");

        // ✅ Verify WhatsApp message was sent
        wa.Verify(w =>
                w.SendQuickReplyAsync(
                    It.Is<string>(p => p == "234"),
                    It.Is<string>(t => t == "Your top payees"),
                    It.IsAny<string>(),
                    It.IsAny<string[]>()),
            Times.Once);

        // ✅ Verify event published
        (await harness.Published.Any<QuickReplySent>()).Should().BeTrue();

        await harness.Stop();
    }

    [Fact]
    public async Task NudgeCmd_Should_Publish_NudgeSent()
    {
        var wa = new Mock<IWhatsAppService>();
        var nudges = new Mock<INudgeService>();
        nudges.Setup(n => n.SelectAsset(NudgeType.TransferFail)).Returns("asset");

        var harness = await TestContextHelper.BuildTestHarness<NudgeCmdConsumer>(services =>
        {
            services.AddSingleton(wa.Object);
            services.AddSingleton(nudges.Object);
            services.AddSingleton(new Mock<IUserService>().Object);
        });

        await harness.Bus.Publish(new NudgeCmd(Guid.NewGuid(), NudgeType.TransferFail, "+234", "text"));

        // Ensure consumer actually handled it
        Assert.True(await harness.Consumed.Any<NudgeCmd>(), "NudgeCmd was not consumed");

        // Verify WhatsApp message was sent
        wa.Verify(w => w.SendTextMessageAsync("+234", "text"), Times.Once);

        // Verify event was published
        (await harness.Published.Any<NudgeSent>()).Should().BeTrue();

        await harness.Stop();
    }


    [Fact]
    public async Task VoiceMessageCmd_Should_Publish_Transcribed()
    {
        var speech = new Mock<ISpeechService>();
        speech.Setup(s => s.TranscribeAsync(It.IsAny<Stream>()))
            .ReturnsAsync(("hi", "en"));

        var handler = new MockHttpMessageHandler("dummy audio");
        var client = new HttpClient(handler);

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

        var harness = await TestContextHelper.BuildTestHarness<VoiceMessageCmdConsumer>(services =>
        {
            services.AddSingleton(speech.Object);
            services.AddSingleton(factory.Object);
        });

        await harness.Bus.Publish(new VoiceMessageCmd(Guid.NewGuid(), "http://fake.url/audio", "+234"));

        // ✅ Confirm consumer ran
        Assert.True(await harness.Consumed.Any<VoiceMessageCmd>(), "VoiceMessageCmd was not consumed");

        // ✅ Confirm event was published
        Assert.True(await harness.Published.Any<VoiceMessageTranscribed>(), "VoiceMessageTranscribed was not published");

        await harness.Stop();
    }


    [Fact]
    public async Task ImageUploadedCmd_Should_Publish_OcrResult()
    {
        var ocr = new Mock<IOcrService>();
        ocr.Setup(o => o.ExtractTextAsync(It.IsAny<Stream>())).ReturnsAsync("text");

        var handler = new MockHttpMessageHandler("fake image content");
        var client = new HttpClient(handler);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

        var harness = await TestContextHelper.BuildTestHarness<ImageUploadedCmdConsumer>(services =>
        {
            services.AddSingleton(ocr.Object);
            services.AddSingleton(factory.Object);
        });

        await harness.Bus.Publish(new ImageUploadedCmd(Guid.NewGuid(), "http://fake-url", "+234"));

        // Ensure message was consumed
        Assert.True(await harness.Consumed.Any<ImageUploadedCmd>(), "ImageUploadedCmd was not consumed");

        // Assert OCR result was published
        (await harness.Published.Any<OcrResultAvailable>()).Should().BeTrue();

        await harness.Stop();
    }


    [Fact]
    public async Task ResolveQuickReplyCmd_Should_Publish_UserIntent_When_Found()
    {
        var userId = Guid.NewGuid();
        var harness = await TestContextHelper.BuildTestHarness<ResolveQuickReplyCmdConsumer>();
        var db = harness.Scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Payees.AddAsync(new Payee
            { Id = Guid.NewGuid(), UserId = userId, AccountNumber = "1", BankCode = "001", Nickname = "joe" });
        await db.SaveChangesAsync();

        await harness.Bus.Publish(new ResolveQuickReplyCmd(userId, "joe"));

        (await harness.Published.Any<UserIntentDetected>()).Should().BeTrue();
        await harness.Stop();
    }

    [Fact]
    public async Task ResolveQuickReplyCmd_Should_Publish_Nudge_When_Not_Found()
    {
        var userId = Guid.NewGuid();
        var harness = await TestContextHelper.BuildTestHarness<ResolveQuickReplyCmdConsumer>();

        await harness.Bus.Publish(new ResolveQuickReplyCmd(userId, "missing"));

        var nudge = harness.Published.Select<NudgeCmd>().FirstOrDefault(x =>
            x.Context.Message.CorrelationId == userId);
        Assert.NotNull(nudge);
        Assert.Equal(NudgeType.TransferFail, nudge.Context.Message.NudgeType);

        await harness.Stop();
    }

    [Fact]
    public async Task RespondWithVoiceCmd_Should_Publish_VoiceReplyReady()
    {
        var tts = new Mock<ITextToSpeechService>();
        tts.Setup(t => t.SynthesizeAsync("hi", "en")).ReturnsAsync(new MemoryStream());

        var harness = await TestContextHelper.BuildTestHarness<RespondWithVoiceCmdConsumer>(services =>
        {
            services.AddSingleton(tts.Object);
        });

        await harness.Bus.Publish(new RespondWithVoiceCmd(Guid.NewGuid(), "hi"));

        (await harness.Published.Any<VoiceReplyReady>()).Should().BeTrue();
        await harness.Stop();
    }

    [Fact]
    public async Task FeedbackCmd_Should_Publish_Logged()
    {
        var svc = new Mock<IFeedbackService>();
        svc.Setup(f => f.StoreAsync(It.IsAny<Guid>(), It.IsAny<FeedbackPayload>())).ReturnsAsync(Guid.NewGuid());

        var harness = await TestContextHelper.BuildTestHarness<FeedbackCmdConsumer>(services =>
        {
            services.AddSingleton(svc.Object);
        });

        await harness.Bus.Publish(new FeedbackCmd(Guid.NewGuid(), new FeedbackPayload(5, "ok")));

        (await harness.Published.Any<FeedbackLogged>()).Should().BeTrue();
        await harness.Stop();
    }

    [Fact]
    public async Task MemoCmd_Should_Publish_MemoSaved()
    {
        var svc = new Mock<IMemoService>();
        svc.Setup(m => m.SaveAsync(It.IsAny<Guid>(), It.IsAny<MemoPayload>())).ReturnsAsync(Guid.NewGuid());

        var harness = await TestContextHelper.BuildTestHarness<MemoCmdConsumer>(services =>
        {
            services.AddSingleton(svc.Object);
        });

        await harness.Bus.Publish(new MemoCmd(Guid.NewGuid(), new MemoPayload(Guid.NewGuid(), "memo", null)));

        (await harness.Published.Any<MemoSaved>()).Should().BeTrue();
        await harness.Stop();
    }

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _content;

        public MockHttpMessageHandler(string content)
        {
            _content = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                { Content = new StringContent(_content) });
        }
    }
}