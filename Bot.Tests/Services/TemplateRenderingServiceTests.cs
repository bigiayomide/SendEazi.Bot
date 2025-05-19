using Bot.Core.Models;
using Bot.Core.Services;
using Bot.Shared.DTOs;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Bot.Tests.Services;

public class TemplateRenderingServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly TemplateRenderingService _service;

    public TemplateRenderingServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        var opts = Options.Create(new TemplateSettings { TemplatesPath = _tempDir });
        _service = new TemplateRenderingService(opts, Mock.Of<ILogger<TemplateRenderingService>>());
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public async Task RenderAsync_Should_Render_Template()
    {
        var tplPath = Path.Combine(_tempDir, "hello.scriban");
        await File.WriteAllTextAsync(tplPath, "Hello {{name}}!");
        var result = await _service.RenderAsync("hello", new { name = "Joe" });
        result.Should().Be("Hello Joe!");
    }

    [Fact]
    public async Task RenderAsync_Should_Return_Error_When_Template_Missing()
    {
        var result = await _service.RenderAsync("missing", new { });
        result.Should().Be("⚠ Template error");
    }

    [Fact]
    public async Task RenderAsync_Should_ReUse_Cached_Template()
    {
        var tplPath = Path.Combine(_tempDir, "cached.scriban");
        await File.WriteAllTextAsync(tplPath, "Hi {{name}}");
        var first = await _service.RenderAsync("cached", new { name = "A" });
        first.Should().Be("Hi A");

        await File.WriteAllTextAsync(tplPath, "changed");

        var second = await _service.RenderAsync("cached", new { name = "B" });
        second.Should().Be("Hi B");
    }

    [Fact]
    public void MaskSensitiveData_Should_Mask_Long_Digits()
    {
        var result = _service.MaskSensitiveData("code 1234567890 and 12345");
        result.Should().Be("code ********** and 12345");
    }

    [Fact]
    public void RenderTransactionPreview_Should_Format_Text()
    {
        var preview = new TransactionPreviewModel
        {
            PayeeName = "1234567890",
            Amount = 1000m,
            Fee = 50m,
            NewBalance = 5000m,
            Timestamp = new DateTime(2024, 1, 2, 3, 4, 0)
        };

        var expected =
            "📄 *Transaction Preview*\n" +
            $"• Payee: **********\n" +
            $"• Amount: ₦{preview.Amount:N2}\n" +
            $"• Fee: ₦{preview.Fee:N2}\n" +
            $"• New Balance: ₦{preview.NewBalance:N2}\n" +
            $"• Time: {preview.Timestamp:yyyy-MM-dd HH:mm}\n";

        var result = _service.RenderTransactionPreview(preview);

        result.Should().Be(expected);
    }
}
