using Bot.Core.Services;
using Bot.Shared.Enums;

namespace Bot.Core.StateMachine.Helpers;

public static class SessionHelper
{
    public static async Task SetSessionState(this IConversationStateService svc, Guid sessionId, ConversationState state)
    {
        await svc.SetStateAsync(sessionId, state);
    }

    public static Task<ConversationState> GetSessionState(this IConversationStateService svc, Guid sessionId)
    {
        return svc.GetStateAsync(sessionId);
    }
}