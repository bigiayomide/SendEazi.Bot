using Bot.Core.Services;

namespace Bot.Tests.TestUtilities;

public class FakeConversationStateService : IConversationStateService
{
    public Task<string> GetStateAsync(Guid sessionId)
    {
        return Task.FromResult("Ready");
    }

    public Task SetStateAsync(Guid sessionId, string state)
    {
        Console.WriteLine($"[Fake] Set session {sessionId} to {state}");
        return Task.CompletedTask;
    }

    public Task SetUserAsync(Guid sessionId, Guid userId)
    {
        Console.WriteLine($"[Fake] Set user {userId} for session {sessionId}");
        return Task.CompletedTask;
    }

    public Task<ConversationSession> GetOrCreateSessionAsync(string phone)
    {
        return Task.FromResult(new ConversationSession
        {
            PhoneNumber = phone,
            SessionId = Guid.NewGuid(),
            State = "None",
            UserId = Guid.NewGuid(),
            LastUpdatedUtc = DateTime.UtcNow
        });
    }

    public Task<ConversationSession?> GetSessionByUserAsync(Guid userId)
    {
        return Task.FromResult<ConversationSession?>(new ConversationSession
        {
            PhoneNumber = "+2340000000000",
            SessionId = Guid.NewGuid(),
            State = "None",
            UserId = userId,
            LastUpdatedUtc = DateTime.UtcNow
        });
    }

    public Task UpdateLastMessageAsync(Guid sessionId, string message)
    {
        Console.WriteLine($"[Fake] Update message for {sessionId}: {message}");
        return Task.CompletedTask;
    }
}