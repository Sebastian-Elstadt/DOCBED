using App.DocumentVision;
using App.VectorStore;

namespace App.UnitTests;

public class DocumentPageVectorDataTests
{
    [Fact]
    public void FromPageAnalysis_MapsAllFields()
    {
        var docId = Guid.NewGuid();
        var analysis = new DocumentPageAnalysis
        {
            PageNumber = 7,
            ContentSummary = "summary",
            ExtractedText = "body",
            LayoutType = "technical_paper"
        };

        var data = DocumentPageVectorData.FromPageAnalysis(docId, analysis);

        Assert.Equal(docId, data.DocumentId);
        Assert.Equal(7, data.PageNumber);
        Assert.Equal("summary", data.ContentSummary);
        Assert.Equal("body", data.ExtractedText);
        Assert.Equal("technical_paper", data.LayoutType);
    }
}
