using App.DocumentVision;

namespace App.UnitTests;

public class DocumentPageAnalysisTests
{
    [Fact]
    public void GenerateEmbeddingText_StartsWithPassagePrefixForE5()
    {
        var analysis = new DocumentPageAnalysis { PageNumber = 1, ContentSummary = "x" };
        Assert.StartsWith("passage: ", analysis.GenerateEmbeddingText());
    }

    [Fact]
    public void GenerateEmbeddingText_ComposesPageSummaryEntitiesAndChartDescriptions()
    {
        var analysis = new DocumentPageAnalysis
        {
            PageNumber = 3,
            ContentSummary = "Quarterly results.",
            KeyEntities = new List<string> { "Q3 2025", "$1.2B revenue" },
            ChartsDiagrams = new List<DocumentPageAnalysis.ChartDiagram>
            {
                new("chart", "Revenue by segment", "..."),
                new("diagram", "Org chart", "...")
            }
        };

        var text = analysis.GenerateEmbeddingText();

        Assert.Contains("Page 3: Quarterly results.", text);
        Assert.Contains("Q3 2025,$1.2B revenue", text);
        Assert.Contains("Revenue by segment", text);
        Assert.Contains("Org chart", text);
    }

    [Fact]
    public void GenerateEmbeddingText_SkipsBlankChartDescriptions()
    {
        var analysis = new DocumentPageAnalysis
        {
            PageNumber = 1,
            ContentSummary = "s",
            ChartsDiagrams = new List<DocumentPageAnalysis.ChartDiagram>
            {
                new("chart", "kept", ""),
                new("chart", "   ", ""),
                new("chart", "", "")
            }
        };

        var text = analysis.GenerateEmbeddingText();

        Assert.Contains("kept", text);
        var trailing = text[text.LastIndexOf("kept", StringComparison.Ordinal)..];
        Assert.DoesNotContain("\n\n", trailing);
    }
}
