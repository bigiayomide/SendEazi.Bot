using Bot.Core.Services;
using Bot.Core.StateMachine.Helpers;
using FluentAssertions;
using Moq;

namespace Bot.Tests.Helpers;

public class SessionHelperTests
{
    [Fact]
    public async Task SetSessionState_Should_Forward_To_Service()
    {
        var svc = new Mock<IConversationStateService>();
        var id = Guid.NewGuid();

        await SessionHelper.SetSessionState(svc.Object, id, "Ready");

        svc.Verify(s => s.SetStateAsync(id, "Ready"), Times.Once);
    }

    [Fact]
    public async Task GetSessionState_Should_Forward_To_Service()
    {
        var svc = new Mock<IConversationStateService>();
        var id = Guid.NewGuid();
        svc.Setup(s => s.GetStateAsync(id)).ReturnsAsync("Ready");

        var state = await SessionHelper.GetSessionState(svc.Object, id);

        state.Should().Be("Ready");
        svc.Verify(s => s.GetStateAsync(id), Times.Once);
    }
}
