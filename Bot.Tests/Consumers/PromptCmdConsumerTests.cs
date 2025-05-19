using Bot.Core.Services;
using Bot.Core.StateMachine.Consumers.UX;
using Bot.Shared.DTOs;
using MassTransit;
using Moq;

namespace Bot.Tests.Consumers;

public class PromptCmdConsumerTests
{
    [Fact]
    public async Task Should_Send_FullName_Prompt()
    {
        var wa = new Mock<IWhatsAppService>();
        var sessions = new Mock<IConversationStateService>();
        sessions.Setup(s => s.GetSessionByUserAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new ConversationSession
            {
                PhoneNumber = "+2348000000000",
                SessionId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                LastUpdatedUtc = DateTime.UtcNow
            });

        var consumer = new PromptFullNameCmdConsumer(wa.Object, sessions.Object);
        var cmd = new PromptFullNameCmd(Guid.NewGuid());
        var ctx = Mock.Of<ConsumeContext<PromptFullNameCmd>>(c => c.Message == cmd);

        await consumer.Consume(ctx);

        wa.Verify(w => w.SendTextMessageAsync("+2348000000000", It.Is<string>(s => s.Contains("full name"))),
            Times.Once);
    }

    [Fact]
    public async Task Should_Send_Nin_Prompt()
    {
        var wa = new Mock<IWhatsAppService>();
        var sessions = new Mock<IConversationStateService>();
        sessions.Setup(s => s.GetSessionByUserAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new ConversationSession
            {
                PhoneNumber = "+2348000000001",
                SessionId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                LastUpdatedUtc = DateTime.UtcNow
            });

        var consumer = new PromptNinCmdConsumer(wa.Object, sessions.Object);
        var cmd = new PromptNinCmd(Guid.NewGuid());
        var ctx = Mock.Of<ConsumeContext<PromptNinCmd>>(c => c.Message == cmd);

        await consumer.Consume(ctx);

        wa.Verify(w => w.SendTextMessageAsync("+2348000000001", It.Is<string>(s => s.Contains("NIN"))), Times.Once);
    }

    [Fact]
    public async Task Should_Send_Bvn_Prompt()
    {
        var wa = new Mock<IWhatsAppService>();
        var sessions = new Mock<IConversationStateService>();
        sessions.Setup(s => s.GetSessionByUserAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new ConversationSession
            {
                PhoneNumber = "+2348000000002",
                SessionId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                LastUpdatedUtc = DateTime.UtcNow
            });

        var consumer = new PromptBvnCmdConsumer(wa.Object, sessions.Object);
        var cmd = new PromptBvnCmd(Guid.NewGuid());
        var ctx = Mock.Of<ConsumeContext<PromptBvnCmd>>(c => c.Message == cmd);

        await consumer.Consume(ctx);

        wa.Verify(w => w.SendTextMessageAsync("+2348000000002", It.Is<string>(s => s.Contains("BVN"))), Times.Once);
    }
}