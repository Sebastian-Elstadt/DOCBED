using App.Abstractions;
using App.DocumentVision;
using App.VectorStore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace App.UnitTests;

public class DocumentPipelineTests
{
    private static DocumentPageAnalysis Page(int n) => new()
    {
        PageNumber = n,
        ContentSummary = $"summary {n}",
        PageHash = BitConverter.GetBytes(n)
    };

    private static async IAsyncEnumerable<T> ToAsync<T>(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            yield return item;
            await Task.Yield();
        }
    }

    [Fact]
    public async Task IngestAsync_FlushesEachFullBatchOfTwoPages()
    {
        var pages = new[] { Page(1), Page(2), Page(3), Page(4) };

        var vision = Substitute.For<IDocumentVisionService>();
        vision.AnalyzeDocument(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<AnalyzeDocumentOptions>())
            .Returns(new DocumentAnalysis
            {
                DocumentHash = new byte[] { 0xAA, 0xBB },
                AnalyzePages = _ => ToAsync(pages)
            });

        var embedding = Substitute.For<IEmbeddingService>();
        embedding.GenerateEmbeddingsAsync(Arg.Any<string[]>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<IEnumerable<double[]>>(new[] { new[] { 0.1 }, new[] { 0.2 } }));

        var store = Substitute.For<IVectorStore<DocumentPageVectorData>>();

        var pipeline = new DocumentPipeline.DocumentPipeline(vision, embedding, store, NullLogger<DocumentPipeline.DocumentPipeline>.Instance);

        using var stream = new MemoryStream();
        await pipeline.IngestAsync(stream, "doc.pdf");

        await embedding.Received(2).GenerateEmbeddingsAsync(Arg.Is<string[]>(arr => arr.Length == 2), Arg.Any<CancellationToken>());
        await store.Received(2).UpsertAsync(Arg.Is<IReadOnlyList<VectorPoint<DocumentPageVectorData>>>(p => p.Count == 2), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IngestAsync_FlushesPartialFinalBatch()
    {
        var pages = new[] { Page(1), Page(2), Page(3) };

        var vision = Substitute.For<IDocumentVisionService>();
        vision.AnalyzeDocument(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<AnalyzeDocumentOptions>())
            .Returns(new DocumentAnalysis
            {
                DocumentHash = new byte[] { 0x01 },
                AnalyzePages = _ => ToAsync(pages)
            });

        var embedding = Substitute.For<IEmbeddingService>();
        embedding.GenerateEmbeddingsAsync(Arg.Is<string[]>(a => a.Length == 2), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<double[]>>(new[] { new[] { 0.1 }, new[] { 0.2 } }));
        embedding.GenerateEmbeddingsAsync(Arg.Is<string[]>(a => a.Length == 1), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<double[]>>(new[] { new[] { 0.3 } }));

        var store = Substitute.For<IVectorStore<DocumentPageVectorData>>();

        var pipeline = new DocumentPipeline.DocumentPipeline(vision, embedding, store, NullLogger<DocumentPipeline.DocumentPipeline>.Instance);

        await pipeline.IngestAsync(new MemoryStream(), "doc.pdf");

        await store.Received(1).UpsertAsync(Arg.Is<IReadOnlyList<VectorPoint<DocumentPageVectorData>>>(p => p.Count == 2), Arg.Any<CancellationToken>());
        await store.Received(1).UpsertAsync(Arg.Is<IReadOnlyList<VectorPoint<DocumentPageVectorData>>>(p => p.Count == 1), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IngestAsync_DoesNotFlushWhenNoPages()
    {
        var vision = Substitute.For<IDocumentVisionService>();
        vision.AnalyzeDocument(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<AnalyzeDocumentOptions>())
            .Returns(new DocumentAnalysis
            {
                DocumentHash = new byte[] { 0x01 },
                AnalyzePages = _ => ToAsync(Array.Empty<DocumentPageAnalysis>())
            });

        var embedding = Substitute.For<IEmbeddingService>();
        var store = Substitute.For<IVectorStore<DocumentPageVectorData>>();

        var pipeline = new DocumentPipeline.DocumentPipeline(vision, embedding, store, NullLogger<DocumentPipeline.DocumentPipeline>.Instance);

        await pipeline.IngestAsync(new MemoryStream(), "doc.pdf");

        await embedding.DidNotReceive().GenerateEmbeddingsAsync(Arg.Any<string[]>(), Arg.Any<CancellationToken>());
        await store.DidNotReceive().UpsertAsync(Arg.Any<IReadOnlyList<VectorPoint<DocumentPageVectorData>>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IngestAsync_ZipsEmbeddingsWithPagesIntoVectorPoints()
    {
        var pageA = Page(1);
        var pageB = Page(2);

        var vision = Substitute.For<IDocumentVisionService>();
        vision.AnalyzeDocument(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<AnalyzeDocumentOptions>())
            .Returns(new DocumentAnalysis
            {
                DocumentHash = new byte[] { 0xCC },
                AnalyzePages = _ => ToAsync(new[] { pageA, pageB })
            });

        var vecA = new[] { 1.0, 2.0 };
        var vecB = new[] { 3.0, 4.0 };
        var embedding = Substitute.For<IEmbeddingService>();
        embedding.GenerateEmbeddingsAsync(Arg.Any<string[]>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<double[]>>(new[] { vecA, vecB }));

        var store = Substitute.For<IVectorStore<DocumentPageVectorData>>();
        IReadOnlyList<VectorPoint<DocumentPageVectorData>>? captured = null;
        store.UpsertAsync(Arg.Do<IReadOnlyList<VectorPoint<DocumentPageVectorData>>>(p => captured = p), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var pipeline = new DocumentPipeline.DocumentPipeline(vision, embedding, store, NullLogger<DocumentPipeline.DocumentPipeline>.Instance);

        await pipeline.IngestAsync(new MemoryStream(), "doc.pdf");

        Assert.NotNull(captured);
        Assert.Equal(2, captured!.Count);
        Assert.Equal(vecA, captured[0].Vector);
        Assert.Equal(vecB, captured[1].Vector);
        Assert.Equal(pageA.PageNumber, captured[0].Data.PageNumber);
        Assert.Equal(pageB.PageNumber, captured[1].Data.PageNumber);
    }

    [Fact]
    public async Task SearchAsync_PrefixesQueryWithE5QueryTokenAndReturnsHits()
    {
        var vision = Substitute.For<IDocumentVisionService>();
        var embedding = Substitute.For<IEmbeddingService>();
        var store = Substitute.For<IVectorStore<DocumentPageVectorData>>();

        var queryVector = new[] { 0.1, 0.2 };
        embedding.GenerateEmbeddingsAsync(Arg.Any<string[]>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<double[]>>(new[] { queryVector }));

        var hits = new List<VectorSearchHit<DocumentPageVectorData>>
        {
            new(Guid.NewGuid(), 0.9f, new DocumentPageVectorData(Guid.NewGuid(), 1, "s", "t", "l"))
        };
        store.SearchAsync(queryVector, 5, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<VectorSearchHit<DocumentPageVectorData>>>(hits));

        var pipeline = new DocumentPipeline.DocumentPipeline(vision, embedding, store, NullLogger<DocumentPipeline.DocumentPipeline>.Instance);

        var result = await pipeline.SearchAsync("what is the revenue?");

        Assert.Same(hits, result);
        await embedding.Received(1).GenerateEmbeddingsAsync(
            Arg.Is<string[]>(a => a.Length == 1 && a[0] == "query: what is the revenue?"),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task SearchAsync_PassesDocumentFilterAndTopKThrough()
    {
        var vision = Substitute.For<IDocumentVisionService>();
        var embedding = Substitute.For<IEmbeddingService>();
        var store = Substitute.For<IVectorStore<DocumentPageVectorData>>();

        embedding.GenerateEmbeddingsAsync(Arg.Any<string[]>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<double[]>>(new[] { new[] { 0.1 } }));
        store.SearchAsync(Arg.Any<double[]>(), Arg.Any<int>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<VectorSearchHit<DocumentPageVectorData>>>(Array.Empty<VectorSearchHit<DocumentPageVectorData>>()));

        var pipeline = new DocumentPipeline.DocumentPipeline(vision, embedding, store, NullLogger<DocumentPipeline.DocumentPipeline>.Instance);

        var docId = Guid.NewGuid();
        await pipeline.SearchAsync("q", topK: 12, filterByDocId: docId);

        await store.Received(1).SearchAsync(Arg.Any<double[]>(), 12, docId, Arg.Any<CancellationToken>());
    }
}
