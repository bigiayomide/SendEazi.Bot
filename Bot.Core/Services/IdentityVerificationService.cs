namespace Bot.Core.Services;

public interface IIdentityVerificationService
{
    Task<bool> VerifyNinAsync(string nin);
    Task<bool> VerifyBvnAsync(string bvn);
}

public class IdentityVerificationService : IIdentityVerificationService
{
    public Task<bool> VerifyNinAsync(string nin) => Task.FromResult(nin.Length == 11);
    public Task<bool> VerifyBvnAsync(string bvn) => Task.FromResult(bvn.Length == 11);
}
