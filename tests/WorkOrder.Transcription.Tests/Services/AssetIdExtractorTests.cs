using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WorkOrder.Transcription.Infrastructure.Services.FieldExtraction;

namespace WorkOrder.Transcription.Tests.Services;

[TestClass]
public class AssetIdExtractorTests
{
    private readonly AssetIdExtractor _extractor = new();

    [TestMethod]
    public void Extract_StandardFormat_FindsAssetId()
    {
        // Arrange
        var text = "Fixed pump TX-482 today";

        // Act
        var result = _extractor.Extract(text);

        // Assert
        result.Should().Contain("TX-482");
    }

    [TestMethod]
    public void Extract_NoDashFormat_FindsAssetId()
    {
        // Arrange
        var text = "Sensor Q5001 is offline";

        // Act
        var result = _extractor.Extract(text);

        // Assert
        result.Should().ContainSingle()
            .Which.Should().Be("Q-5001"); // Normalized with dash
    }

    [TestMethod]
    public void Extract_RollerFormat_FindsAssetId()
    {
        // Arrange
        var text = "Lubricated ROLLER A-5";

        // Act
        var result = _extractor.Extract(text);

        // Assert
        result.Should().Contain("ROLLER A-5");
    }

    [TestMethod]
    public void Extract_MultipleAssets_FindsAll()
    {
        // Arrange
        var text = "Replaced TX-482 and PUMP-17";

        // Act
        var result = _extractor.Extract(text);

        // Assert
        result.Should().HaveCount(2)
            .And.Contain(new[] { "TX-482", "PUMP-17" });
    }

    [TestMethod]
    public void Extract_NoAssets_ReturnsEmpty()
    {
        // Arrange
        var text = "General maintenance work completed";

        // Act
        var result = _extractor.Extract(text);

        // Assert
        result.Should().BeEmpty();
    }
}
