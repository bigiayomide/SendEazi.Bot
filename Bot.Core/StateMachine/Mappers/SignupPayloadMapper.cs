using Bot.Shared;
using Bot.Shared.Models;

namespace Bot.Core.StateMachine.Mappers;

public static class SignupPayloadMapper
{
    public static SignupPayload FromSaga(BotState state)
    {
        return new SignupPayload(
            state.TempName!,
            state.PhoneNumber!,
            state.TempNIN!,
            state.TempBVN!);
    }
}