using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using App.Abstractions;
using App.DocumentConverter;
using Microsoft.Extensions.Logging;

namespace Infra.DocumentConverter;

public sealed class PythonDocumentConverterService(
    HttpClient httpClient,
    ILogger<PythonDocumentConverterService> logger
) : IDocumentConverterService
{
    public async IAsyncEnumerable<DocumentPageConversionResult> ConvertToPageImagesAsync(Stream fileStream, string fileName, [EnumeratorCancellation] CancellationToken ct = default)
    {
        logger.LogInformation($"Converting input file '{fileName}' to page images...");

        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(fileStream);

        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(streamContent, "file", fileName);

        var request = new HttpRequestMessage(HttpMethod.Post, "convert")
        {
            Content = content
        };

        var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrWhiteSpace(line)) continue;

            var page = JsonSerializer.Deserialize<DocumentPageConversionResult>(line,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });

            if (page is null)
            {
                logger.LogWarning("Converted document page returned null.");
                continue;
            }

            logger.LogInformation($"Converted page {page.Page}.");
            yield return page;
        }
    }
}