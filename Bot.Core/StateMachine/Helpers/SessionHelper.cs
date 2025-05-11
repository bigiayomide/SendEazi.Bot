using Bot.Core.Services;

namespace Bot.Core.StateMachine.Helpers;

public static class SessionHelper
{
    public static async Task SetSessionState(this IConversationStateService svc, Guid sessionId, string state)
    {
        await svc.SetStateAsync(sessionId, state);
    }

    public static Task<string> GetSessionState(this IConversationStateService svc, Guid sessionId)
    {
        return svc.GetStateAsync(sessionId);
    }
}