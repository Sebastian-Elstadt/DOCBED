using App.Abstractions;
using App.VectorStore;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace Infra.VectorStore;

public class QdrantDocumentPagesVectorStore(QdrantClient client) : IVectorStore<DocumentPageVectorData>
{
    private const string CollectionName = "document_pages";
    private const ulong VectorDim = 1024;

    public async Task EnsureCollectionAsync(CancellationToken ct = default)
    {
        bool exists = await client.CollectionExistsAsync(CollectionName, ct);
        if (exists) return;

        await client.CreateCollectionAsync(
            collectionName: CollectionName,
            vectorsConfig: new VectorParams
            {
                Size = VectorDim,
                Distance = Distance.Cosine
            },
            cancellationToken: ct
        );

        await client.CreatePayloadIndexAsync(
            collectionName: CollectionName,
            fieldName: "document_id",
            schemaType: PayloadSchemaType.Uuid,
            cancellationToken: ct
        );
    }

    public async Task UpsertAsync(IReadOnlyList<VectorPoint<DocumentPageVectorData>> points, CancellationToken ct = default)
    {
        var qdrantPoints = points.Select(p => new PointStruct
        {
            Id = new PointId { Uuid = p.Id.ToString() },
            Vectors = p.Vector.Select(d => (float)d).ToArray(),
            Payload =
            {
                ["document_id"] = p.Data.DocumentId.ToString(),
                ["page_number"] = p.Data.PageNumber,
                ["content_summary"] = p.Data.ContentSummary,
                ["extracted_text"] = p.Data.ExtractedText,
                ["layout_type"] = p.Data.LayoutType
            }
        }).ToList();

        await client.UpsertAsync(CollectionName, qdrantPoints, cancellationToken: ct);
    }

    public async Task<IReadOnlyList<VectorSearchHit<DocumentPageVectorData>>> SearchAsync(double[] queryVector, int topK, Guid? filterByDocId, CancellationToken ct = default)
    {
        Filter? filter = null;
        if (filterByDocId is { } docId)
        {
            filter = new Filter
            {
                Must =
                {
                    new Condition
                    {
                        Field = new FieldCondition
                        {
                            Key = "document_id",
                            Match  = new Match { Keyword = docId.ToString() }
                        }
                    }
                }
            };
        }

        var results = await client.SearchAsync(
            collectionName: CollectionName,
            vector: queryVector.Select(d => (float)d).ToArray(),
            limit: (ulong)topK,
            filter: filter,
            cancellationToken: ct
        );

        return results.Select(r => new VectorSearchHit<DocumentPageVectorData>(
            Id: Guid.Parse(r.Id.Uuid),
            Score: r.Score,
            Data: new DocumentPageVectorData(
                DocumentId: Guid.Parse(r.Payload["document_id"].StringValue),
                PageNumber: (int)r.Payload["page_number"].IntegerValue,
                ContentSummary: r.Payload["content_summary"].StringValue,
                ExtractedText: r.Payload["extracted_text"].StringValue,
                LayoutType: r.Payload["layout_type"].StringValue
            )
        )).ToList();
    }
}