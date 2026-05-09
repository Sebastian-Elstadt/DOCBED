# docbed

A small AI pipeline that turns arbitrary documents (PDF, DOCX, etc.) into a searchable vector store. Each page is rasterized, passed through a vision-language model for structured extraction, embedded, and upserted into Qdrant. Search returns the top-k pages by semantic similarity.

This is a showcase project — built to demonstrate end-to-end AI pipeline engineering against real model and infrastructure constraints, not as a production product.

## What it does

1. Accept a document upload via HTTP (`POST /documents`).
2. Render each page to a JPEG (Python sidecar — `pdf2image` / `pymupdf`).
3. Send each page image to a vision LLM and extract a structured JSON description (summary, text in reading order, tables, charts, key entities, layout type).
4. Build a compact embedding text per page (summary + entities + chart descriptions — deliberately *not* the full extracted text, see below).
5. Embed via `intfloat/multilingual-e5-large-instruct` and upsert into Qdrant with a payload index on `document_id`.
6. Search by free-text query; results are filterable by document.

## Architecture

```
                   ┌──────────────┐
   POST /documents │              │
   ───────────────▶│   API (.NET) │──────────────────────────┐
                   │              │                          │
                   └──────┬───────┘                          │
                          │                                  ▼
                          │                            ┌──────────┐
              page images │   structured JSON          │          │
                          ▼                            │  Qdrant  │
                   ┌──────────────┐                    │          │
                   │ doc-converter│                    └──────────┘
                   │  (Python)    │                          ▲
                   │  pdf2image   │                          │
                   │  pymupdf     │     embeddings           │
                   └──────────────┘    ┌──────────────┐      │
                                       │  TogetherAI  │──────┘
                                       │  vision +    │
                                       │  embedding   │
                                       └──────────────┘
```

Three services orchestrated via `docker-compose`:

| Service          | Stack                          | Role                                                  |
|------------------|--------------------------------|-------------------------------------------------------|
| `api`            | ASP.NET Core (.NET 10)         | HTTP API and pipeline orchestration                   |
| `doc-converter`  | FastAPI + pdf2image + pymupdf  | Streams page-by-page JPEG images as NDJSON           |
| `qdrant`         | Qdrant                         | Vector store with `document_pages` collection (1024-d, cosine) |

External: TogetherAI for the vision LLM and the embedding model.

## The .NET API

Three projects, clean layering:

```
api/
├── Api/         ASP.NET host, controllers, DI composition
├── App/         Domain abstractions and the orchestration pipeline
├── Infra/       External integrations (TogetherAI, Qdrant, doc-converter)
└── Tests/
    ├── Api.UnitTests/      controller-level unit tests
    ├── App.UnitTests/      pipeline batching, embedding text, mapping
    └── Infra.UnitTests/    parsers + HTTP-handler-stub tests
```

`App` defines interfaces (`IDocumentVisionService`, `IEmbeddingService`, `IVectorStore<T>`, `IDocumentConverterService`); `Infra` implements them; `Api` only knows about `IDocumentPipeline`. The pipeline streams page analyses as they arrive from the vision model, batches into groups of two, and flushes each batch through embedding → vector upsert before the next batch arrives.

## Engineering notes

A few decisions that aren't obvious from the code:

**Embedding text is a compact summary, not the full page.**
`intfloat/multilingual-e5-large-instruct` has a hard 512-token context limit. The vision model produces rich page extractions (full reading-order text, table contents, etc.) that frequently exceed that — so the embedding input is built from `content_summary + key_entities + chart descriptions` and prefixed with the e5-required `passage: ` token. The full extracted text remains in the Qdrant payload as a retrieval target. See `App/DocumentVision/DocumentPageAnalysis.cs::GenerateEmbeddingText`.

**Streaming throughout.**
- `doc-converter` streams pages as NDJSON so the API can start vision analysis on page 1 before page N is rendered.
- The vision API call uses SSE streaming.
- `DocumentPipeline.IngestAsync` consumes the page stream as `IAsyncEnumerable<DocumentPageAnalysis>` and flushes batches incrementally.

This keeps memory bounded for large documents and lets the embedding/vector-store call overlap with later vision calls.

**Vision model choice is operator policy.**
The vision integration uses TogetherAI's chat completions endpoint with `response_format: { type: "json_object", schema: <JSON schema> }` for schema-constrained decoding. Schema-constrained output is per-model — reasoning models in particular often ignore it and emit `<think>` traces and draft JSON. To stay robust across operator choices, the response parser (`Infra/DocumentVision/PageAnalysisParser.cs`) accepts plain JSON, fenced JSON, and outer-brace fallback, picking the last candidate when multiple are present.

**Document ID is derived, not assigned.**
Document and page IDs come from hashing content into a GUID (`ByteArrayExtensions.ToGuid`). Re-ingesting the same file is idempotent — same ID, Qdrant upsert overwrites.

## Running locally

Requirements: Docker, Docker Compose, a TogetherAI API key.

```bash
# 1. Create the external network and volume the compose file expects
docker network create docbed
docker volume create docbed_qdrant

# 2. Provide a .env file at the repo root
cat > .env <<EOF
TOGETHERAI_BASE_URL=https://api.together.xyz/
TOGETHERAI_API_KEY=your_key_here
QDRANT_HOST=qdrant
QDRANT_PORT=6334
QDRANT_HTTPS=false
EOF

# 3. Build and start
docker compose up --build
```

The API listens on `http://127.0.0.1:8000`. Ingest a document:

```bash
curl -F "file=@/path/to/document.pdf" http://127.0.0.1:8000/documents
```

## Tests

```bash
cd api
dotnet test
```

30 tests across the three layers — pipeline batching, embedding-text composition, JSON candidate extraction across model output styles, NDJSON streaming, and HTTP request shape verification (no live API calls).

## Repo layout

```
docbed/
├── api/                     .NET API (this is the main artifact)
├── doc-converter/           Python sidecar for page rasterization
├── docker-compose.yml       Three-service orchestration
├── qdrant.prod.yaml         Qdrant config
└── research/                Throwaway scripts and prototypes
```
