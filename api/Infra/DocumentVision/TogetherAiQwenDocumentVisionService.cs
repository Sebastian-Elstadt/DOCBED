using System.Net.Http.Json;
using App.Abstractions;
using App.DocumentVision;

namespace Infra.DocumentVision;

public class TogetherAiQwenDocumentVisionService(
    HttpClient httpClient
) : IDocumentVisionService
{
    public async Task AnalyzeDocumentAsync(FileStream fileStream, AnalyzeDocumentOptions options, CancellationToken ct = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "v1/chat/completions",
            new
            {
                model = "Qwen/Qwen2-VL-72B-Instruct",
                temperature = options.Temperature,
                max_tokens = options.MaxTokens,
                response_format = new { type = "json_object" },
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
                                    url = "data:image/jpeg;base64,"
                                }
                            }
                        ]
                    }
                ]
            },
            ct
        );
    }
}