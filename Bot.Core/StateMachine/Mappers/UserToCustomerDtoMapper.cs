using Bot.Shared.Models;

namespace Bot.Core.StateMachine.Mappers;

public static class UserToCustomerDtoMapper
{
    public static object ToMonoPayload(User user)
    {
        var nameParts = user.FullName.Split(' ', 2);
        return new
        {
            first_name = nameParts[0],
            last_name  = nameParts.Length > 1 ? nameParts[1] : "NA",
            phone      = user.PhoneNumber,
            type       = "individual",
            email      = $"{user.Id}@bot.fake",
            bvn        = "[[DECRYPTED_BVN]]" // placeholder, decrypt before passing
        };
    }

    public static object ToOnePipePayload(User user)
    {
        return new
        {
            name       = user.FullName,
            mobile_no  = user.PhoneNumber.StartsWith("+234")
                ? user.PhoneNumber.Replace("+", "")
                : "234" + user.PhoneNumber,
            bvn        = "[[ENCRYPTED_BVN]]"
        };
    }
}