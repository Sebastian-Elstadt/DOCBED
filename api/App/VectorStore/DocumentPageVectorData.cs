using App.DocumentVision;

namespace App.VectorStore;

public sealed record DocumentPageVectorData(
    Guid DocumentId,
    int PageNumber,
    string ContentSummary,
    string ExtractedText,
    string LayoutType
)
{
    public static DocumentPageVectorData FromPageAnalysis(Guid documentId, DocumentPageAnalysis pageAnalysis)
        => new(
            DocumentId: documentId,
            PageNumber: pageAnalysis.PageNumber,
            ContentSummary: pageAnalysis.ContentSummary,
            ExtractedText: pageAnalysis.ExtractedText,
            LayoutType: pageAnalysis.LayoutType
        );
}