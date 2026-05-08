using App.Abstractions;
using App.DocumentVision;
using App.Extensions;
using App.VectorStore;

namespace App.DocumentPipeline;

public class DocumentPipeline(
    IDocumentVisionService visionService,
    IEmbeddingService embeddingService,
    IVectorStore vectorStore
) : IDocumentPipeline
{
    public async Task IngestAsync(Stream fileStream, string fileName, CancellationToken ct = default)
    {
        const int PagesBatchSize = 16;
        var visionOptions = new AnalyzeDocumentOptions(Temperature: 0.2, MaxTokens: 8000);

        var analysis = visionService.AnalyzeDocument(fileStream, fileName, visionOptions);
        var documentId = analysis.DocumentHash.ToGuid();

        var pagesBatch = new List<DocumentPageAnalysis>(PagesBatchSize);

        await foreach (var page in analysis.AnalyzePages(ct))
        {
            pagesBatch.Add(page);
            if (pagesBatch.Count >= PagesBatchSize)
            {
                await FlushAsync(documentId, pagesBatch, ct);
                pagesBatch.Clear();
            }
        }

        if (pagesBatch.Count > 0)
            await FlushAsync(documentId, pagesBatch, ct);
    }

    private async Task FlushAsync(Guid documentId, List<DocumentPageAnalysis> pages, CancellationToken ct = default)
    {
        var inputs = pages.Select(p => p.GenerateEmbeddingText()).ToArray();
        var vectors = (await embeddingService.GenerateEmbeddingsAsync(inputs, ct)).ToArray();

        var points = pages.Zip(vectors, (p, v) => new VectorPoint<DocumentPageVectorData>(
            Id: p.PageHash.ToGuid(),
            Vector: v,
            Data: DocumentPageVectorData.FromPageAnalysis(documentId, p)
        )).ToList();

        await vectorStore.UpsertAsync(points, ct);
    }

    public async Task<IReadOnlyList<VectorSearchHit<DocumentPageVectorData>>> SearchAsync(string query, int topK = 5, Guid? filterByDocId = null, CancellationToken ct = default)
    {
        var vector = (await embeddingService.GenerateEmbeddingsAsync(["query: " + query], ct)).Single();
        return await vectorStore.SearchAsync<DocumentPageVectorData>(vector, topK, filterByDocId, ct);
    }
}