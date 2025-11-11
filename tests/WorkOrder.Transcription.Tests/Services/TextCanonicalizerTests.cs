using Microsoft.VisualStudio.TestTools.UnitTesting;
using WorkOrder.Transcription.Infrastructure.Services.FieldExtraction;

namespace WorkOrder.Transcription.Tests.Services;

[TestClass]
public class TextCanonicalizerTests
{
    private readonly TextCanonicalizer _canonicalizer = new();

    [TestMethod]
    [DataRow("T X dash four eight two", "en-GB", "TX-482")]
    [DataRow("PUMP seventeen", "en-GB", "PUMP-17")]
    [DataRow("Q five zero zero one", "en-GB", "Q-5001")]
    [DataRow("ROLLER A dash five", "en-GB", "ROLLER A-5")]
    [DataRow("TX bindestreck fyra åtta två", "sv-SE", "TX-482")]
    [DataRow("PUMP tiret dix sept", "fr-FR", "PUMP-17")]
    public void Canonicalize_VariousFormats_NormalizesCorrectly(
        string input, string locale, string expected)
    {
        // Act
        var result = _canonicalizer.Canonicalize(input, locale);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void Canonicalize_MultipleSpaces_CollapsesToSingle()
    {
        // Arrange
        var input = "TX    482   needs    repair";

        // Act
        var result = _canonicalizer.Canonicalize(input, "en-GB");

        // Assert
        Assert.IsFalse(result.Contains("  ")); // No double spaces
    }

    [TestMethod]
    public void Canonicalize_UnsupportedLocale_FallsBackToEnglish()
    {
        // Arrange
        var input = "Q five zero zero one";

        // Act
        var result = _canonicalizer.Canonicalize(input, "xx-XX");

        // Assert
        Assert.AreEqual("Q-5001", result);
    }
}
