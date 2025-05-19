using Bot.Core.StateMachine.Mappers;
using Bot.Shared.Models;
using FluentAssertions;

namespace Bot.Tests.Mappers;

public class UserToCustomerDtoMapperTests
{
    [Fact]
    public void ToMonoPayload_Should_Split_Name_And_Set_Placeholders()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "John Doe",
            PhoneNumber = "+2348012345678"
        };

        var payload = UserToCustomerDtoMapper.ToMonoPayload(user);

        payload.Should().BeEquivalentTo(new
        {
            first_name = "John",
            last_name = "Doe",
            phone = "+2348012345678",
            type = "individual",
            email = $"{user.Id}@bot.fake",
            bvn = "[[DECRYPTED_BVN]]"
        });
    }

    [Fact]
    public void ToOnePipePayload_Should_Format_Phone_And_Bvn()
    {
        var user = new User
        {
            FullName = "John Doe",
            PhoneNumber = "+2348012345678"
        };

        var payload = UserToCustomerDtoMapper.ToOnePipePayload(user);

        payload.Should().BeEquivalentTo(new
        {
            name = "John Doe",
            mobile_no = "2348012345678",
            bvn = "[[ENCRYPTED_BVN]]"
        });
    }
}