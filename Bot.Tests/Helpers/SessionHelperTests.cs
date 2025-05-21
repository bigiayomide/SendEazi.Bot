using Bot.Core.Services;
using Bot.Core.StateMachine.Helpers;
using Bot.Shared.Enums;
using FluentAssertions;
using Moq;

namespace Bot.Tests.Helpers;

public class SessionHelperTests
{
    [Fact]
    public async Task SetSessionState_Should_Call_Service_With_Session_Id()
    {
        var svc = new Mock<IConversationStateService>(MockBehavior.Strict);
        var id = Guid.NewGuid();

        svc.Setup(s => s.SetStateAsync(id, ConversationState.Ready)).Returns(Task.CompletedTask).Verifiable();

        await svc.Object.SetSessionState(id, ConversationState.Ready);

        svc.Verify();
    }

    [Fact]
    public async Task GetSessionState_Should_Call_Service_With_Session_Id()
    {
        var svc = new Mock<IConversationStateService>(MockBehavior.Strict);
        var id = Guid.NewGuid();
        svc.Setup(s => s.GetStateAsync(id)).ReturnsAsync(ConversationState.Ready).Verifiable();

        var state = await svc.Object.GetSessionState(id);

        state.Should().Be(ConversationState.Ready);
        svc.Verify();
    }
}