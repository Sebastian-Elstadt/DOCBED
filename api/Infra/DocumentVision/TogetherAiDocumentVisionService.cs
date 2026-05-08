using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using App.Abstractions;
using App.DocumentConverter;
using App.DocumentVision;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace Infra.DocumentVision;

public sealed class TogetherAiDocumentVisionService(
    HttpClient httpClient,
    ILogger<TogetherAiDocumentVisionService> logger,
    IDocumentConverterService docConverterService
) : IDocumentVisionService
{
    private static readonly Regex JsonFenceRegex = new(
        @"```(?:json)?\s*(\{.*?\})\s*```",
        RegexOptions.Singleline | RegexOptions.Compiled
    );

    private DocumentPageAnalysis ParsePageAnalysis(string response)
    {
        string? candidate = null;

        var matches = JsonFenceRegex.Matches(response);
        if (matches.Count > 0)
        {
            candidate = matches[^1].Groups[1].Value;
        }
        else
        {
            var start = response.IndexOf('{');
            var end = response.LastIndexOf('}');
            if (start >= 0 && end > start)
            {
                candidate = response[start..(end + 1)];
            }
        }

        if (candidate is null)
            throw new InvalidOperationException($"No JSON found in response: {response}");

        return JsonSerializer.Deserialize<DocumentPageAnalysis>(candidate)
            ?? throw new InvalidOperationException($"Failed to deserialize: {candidate}");
    }

    private async Task<DocumentPageAnalysis> AnalyzePageImageAsync(DocumentPageConversionResult page, AnalyzeDocumentOptions options, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "v1/chat/completions")
        {
            Content = JsonContent.Create(new
            {
                model = "moonshotai/Kimi-K2.6",
                temperature = options.Temperature,
                max_tokens = options.MaxTokens,
                stream = true,
                messages = (object[])[
                    new {
                        role = "system",
                        content = "You are an expert document analysis model. You will be given one page from a document. Extract all information with high accuracy. Parse tables properly, describe charts and diagrams in detail, maintain reading order, and output only valid JSON."
                    },
                    new {
                        role = "user",
                        content = (object[])[
                            new {
                                type = "text",
                                text = """
                                Analyze this document page and return a detailed JSON object using this exact structure:

                                {
                                    "page_number": 1,
                                    "content_summary": "one sentence summary",
                                    "extracted_text": "all readable text in reading order",
                                    "tables": [array of tables as arrays or objects],
                                    "charts_diagrams": [
                                        {
                                            "type": "chart|diagram|graph|infographic",
                                            "description": "detailed description including title, axis labels, key insights",
                                            "data": "extracted data if possible"
                                        }
                                    ],
                                    "key_entities": ["list of important names, numbers, dates, findings"],
                                    "layout_type": "financial_report | technical_paper | presentation | legal | other"
                                }

                                Be precise, structured, and detailed.
                                """
                            },
                            new {
                                type = "image_url",
                                image_url = new {
                                    url = "data:image/jpeg;base64," + page.ImageBase64
                                }
                            }
                        ]
                    }
                ]
            })
        };

        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

        if (!response.IsSuccessStatusCode)
        {
            var res = await response.Content.ReadAsStringAsync(ct);
            throw new Exception("TogetherAI Request failed: " + res);
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);
        var sb = new StringBuilder();

        while (await reader.ReadLineAsync(ct) is { } line)
        {
            if (string.IsNullOrEmpty(line) || !line.StartsWith("data: ")) continue;

            var payload = line["data: ".Length..];
            if (payload == "[DONE]") break;

            using var doc = JsonDocument.Parse(payload);
            var choices = doc.RootElement.GetProperty("choices");
            if (choices.GetArrayLength() == 0) continue;

            var delta = choices[0].GetProperty("delta");
            if (delta.TryGetProperty("content", out var c) && c.ValueKind == JsonValueKind.String)
                sb.Append(c.GetString());
        }

        return ParsePageAnalysis(sb.ToString());
    }

    private async IAsyncEnumerable<DocumentPageAnalysis> AnalyzePagesIterator(Stream fileStream, string fileName, AnalyzeDocumentOptions options, [EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var page in docConverterService.ConvertToPageImagesAsync(fileStream, fileName, ct))
        {
            if (!page.Success)
            {
                logger.LogError($"Error on document '{fileName}' page {page.Page}: {page.Error}");
                continue;
            }

            if (string.IsNullOrWhiteSpace(page.ImageBase64))
            {
                logger.LogError($"Error on document '{fileName}' page {page.Page}: No Base64 returned.");
                continue;
            }

            yield return await AnalyzePageImageAsync(page, options, ct);
        }
    }

    public DocumentAnalysis AnalyzeDocument(Stream fileStream, string fileName, AnalyzeDocumentOptions options)
    {
        return new()
        {
            DocumentHash = [],
            AnalyzePages = (ct) => AnalyzePagesIterator(fileStream, fileName, options, ct)
        };
    }
}