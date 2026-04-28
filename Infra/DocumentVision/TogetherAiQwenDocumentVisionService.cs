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
                        content = "You are an expert document intelligence model. Analyze the image of a document page with extreme precision. Extract all text accurately, describe charts and diagrams in detail, parse tables into structured rows/columns, and understand layout and hierarchy. Always respond with valid JSON only."
                    },
                    new {
                        role = "user",
                        content = (object[])[
                            new {
                                type = "text",
                                text = "Analyze this document page thoroughly.\n\nReturn a detailed JSON object with this exact structure:\n{\n  \"page_summary\": \"...\",\n  \"text_content\": \"full extracted text\",\n  \"tables\": [array of parsed tables],\n  \"charts\": [array of detailed chart descriptions],\n  \"diagrams\": [array of diagram descriptions],\n  \"key_findings\": [\"list of important points\"],\n  \"layout_type\": \"report | technical | financial | academic | other\"\n}\n\nBe precise and detailed."
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