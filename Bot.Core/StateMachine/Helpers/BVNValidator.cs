namespace Bot.Core.StateMachine.Helpers;

public static class BvnValidator
{
    public static bool IsValid(string bvn)
    {
        return bvn.Length == 11 && long.TryParse(bvn, out _);
    }

    public static bool IsNinValid(string nin)
    {
        return nin.Length == 11 && long.TryParse(nin, out _);
    }
}