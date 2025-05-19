using Bot.Core.Models;
using FluentAssertions;

namespace Bot.Tests.Models;

public class QuickReplyOptionsTests
{
    [Fact]
    public void Constructor_Should_Set_Default_Values()
    {
        var opts = new QuickReplyOptions();

        opts.MaxFavorites.Should().Be(3);
        opts.RedisKeyPrefix.Should().Be("qr:");
        opts.DefaultTemplates.Should().Equal("Check balance", "Send money", "Help");
    }
}