using Bot.Core.Services;
using Bot.Core.StateMachine.Consumers.UX;
using Bot.Shared.DTOs;
using Bot.Shared.Models;
using FluentAssertions;
using MassTransit;
using Moq;

namespace Bot.Tests.Consumers;

public class QuickReplyCmdConsumerTests
{
    [Fact]
    public async Task QuickReplyCmd_Should_Throw_When_User_Missing()
    {
        var wa = new Mock<IWhatsAppService>();
        var replies = new Mock<IQuickReplyService>();
        var users = new Mock<IUserService>();
        users.Setup(u => u.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((User?)null);

        var consumer = new QuickReplyCmdConsumer(wa.Object, users.Object, replies.Object);
        var cmd = new QuickReplyCmd(Guid.NewGuid(), "tmpl", new[] { "hello" });
        var ctx = Mock.Of<ConsumeContext<QuickReplyCmd>>(c => c.Message == cmd);

        var act = async () => await consumer.Consume(ctx);

        await act.Should().ThrowAsync<NullReferenceException>();
    }
}
