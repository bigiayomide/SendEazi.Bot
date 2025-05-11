namespace Bot.Core.StateMachine.Helpers;

public static class PhoneUtil
{
    public static string Normalize(string raw)
    {
        var digits = raw.Replace(" ", "").Replace("-", "").TrimStart('0');

        if (digits.StartsWith("+234"))
            return digits;

        if (digits.StartsWith("234"))
            return "+" + digits;

        if (digits.Length == 10)
            return "+234" + digits;

        return digits;
    }
}