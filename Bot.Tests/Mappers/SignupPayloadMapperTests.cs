using Bot.Core.StateMachine.Mappers;
using Bot.Shared;
using Bot.Shared.Models;
using FluentAssertions;

namespace Bot.Tests.Mappers;

public class SignupPayloadMapperTests
{
    [Fact]
    public void FromSaga_Should_Map_TempFields_To_Payload()
    {
        var state = new BotState
        {
            TempName = "Jane Doe",
            PhoneNumber = "+2348111111111",
            TempNIN = "12345678901",
            TempBVN = "10987654321"
        };

        var payload = SignupPayloadMapper.FromSaga(state);

        payload.FullName.Should().Be(state.TempName);
        payload.Phone.Should().Be(state.PhoneNumber);
        payload.NIN.Should().Be(state.TempNIN);
        payload.BVN.Should().Be(state.TempBVN);
    }
}
