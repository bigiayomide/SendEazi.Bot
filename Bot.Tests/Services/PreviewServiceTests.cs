using Bot.Core.Services;
using Bot.Shared.DTOs;
using FluentAssertions;
using Moq;

namespace Bot.Tests.Services;

public class PreviewServiceTests
{
    [Fact]
    public async Task GenerateTransactionPreviewAsync_ForwardsTemplateName()
    {
        // Arrange
        var model = new TransactionPreviewModel
        {
            PayeeName = "John",
            Amount = 1000,
            Fee = 50,
            NewBalance = 5000,
            Timestamp = DateTime.UtcNow
        };

        var templater = new Mock<ITemplateRenderingService>();
        templater.Setup(t => t.RenderAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync("preview");
        var service = new PreviewService(templater.Object);

        // Act
        var result = await service.GenerateTransactionPreviewAsync(model);

        // Assert
        result.Should().Be("preview");
        templater.Verify(t => t.RenderAsync("TransactionPreview", model), Times.Once);
    }
}
