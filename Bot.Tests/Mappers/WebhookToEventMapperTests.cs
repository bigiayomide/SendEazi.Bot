using System.Text.Json;
using Bot.Core.StateMachine.Mappers;
using FluentAssertions;

namespace Bot.Tests.Mappers;

public class WebhookToEventMapperTests
{
    [Fact]
    public void MapTransferSuccess_Should_Extract_UserId_And_Reference()
    {
        var guid = Guid.NewGuid();
        var json = $$"""
        {
            "transaction_ref": "txn:{{guid}}",
            "transaction_id": "abc123"
        }
        """;

        var doc = JsonDocument.Parse(json);
        var evt = WebhookToEventMapper.MapTransferSuccess(doc.RootElement, "Mono");

        evt.CorrelationId.Should().Be(guid);
        evt.Reference.Should().Be($"txn:{guid}");
    }

    [Fact]
    public void MapTransferFailed_Should_Handle_Missing_Reason()
    {
        var guid = Guid.NewGuid();
        var json = $$"""
                     {
            "transaction_ref": "txn:{{guid}}"
        }
        """;

        var doc = JsonDocument.Parse(json);
        var evt = WebhookToEventMapper.MapTransferFailed(doc.RootElement);

        evt.CorrelationId.Should().Be(guid);
        evt.Reference.Should().Be($"txn:{guid}");
        evt.Reason.Should().Be("No reason provided");
    }

    [Fact]
    public void MapTransferFailed_Should_Extract_Reason_When_Present()
    {
        var guid = Guid.NewGuid();
        var json = $$"""
        {
            "transaction_ref": "txn:{{guid}}",
            "failure_reason": "Insufficient funds"
        }
        """;

        var doc = JsonDocument.Parse(json);
        var evt = WebhookToEventMapper.MapTransferFailed(doc.RootElement);

        evt.Reason.Should().Be("Insufficient funds");
    }

    [Fact]
    public void MapMonoMandate_Should_Extract_MandateId_And_UserId()
    {
        var guid = Guid.NewGuid();
        var json = $$"""
        {
            "reference": "mandate:{{guid}}",
            "mandate_id": "mono-m-123"
        }
        """;

        var doc = JsonDocument.Parse(json);
        var evt = WebhookToEventMapper.MapMonoMandate(doc.RootElement);

        evt.CorrelationId.Should().Be(guid);
        evt.MandateId.Should().Be("mono-m-123");
        evt.Provider.Should().Be("Mono");
    }

    [Fact]
    public void MapOnePipeMandate_Should_Parse_Correctly()
    {
        var guid = Guid.NewGuid();
        var json = $$"""
        {
            "transaction_ref": "mandate:{{guid}}",
            "mandate_id": "op-mandate-456"
        }
        """;

        var doc = JsonDocument.Parse(json);
        var evt = WebhookToEventMapper.MapOnePipeMandate(doc.RootElement);

        evt.CorrelationId.Should().Be(guid);
        evt.MandateId.Should().Be("op-mandate-456");
        evt.Provider.Should().Be("OnePipe");
    }

    [Fact]
    public void GetUserId_Should_Return_EmptyGuid_When_Invalid()
    {
        var result = typeof(WebhookToEventMapper)
            .GetMethod("GetUserId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .Invoke(null, ["garbage"]);

        result.Should().Be(Guid.Empty);
    }

    [Fact]
    public void GetUserId_Should_Parse_Valid_Reference()
    {
        var guid = Guid.NewGuid();
        var result = typeof(WebhookToEventMapper)
            .GetMethod("GetUserId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .Invoke(null, [$"txn:{guid}"]);

        result.Should().Be(guid);
    }
}
