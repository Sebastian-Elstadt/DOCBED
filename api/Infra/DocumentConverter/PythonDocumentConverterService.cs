using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using App.Abstractions;
using App.DocumentConverter;

namespace Infra.DocumentConverter;

public sealed class PythonDocumentConverterService(HttpClient httpClient) : IDocumentConverterService
{
    public async IAsyncEnumerable<DocumentPage> ConvertToPageImagesAsync(Stream fileStream, string fileName, [EnumeratorCancellation] CancellationToken ct = default)
    {
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

            var page = JsonSerializer.Deserialize<DocumentPage>(line,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });

            if (page != null)
                yield return page;
        }
    }
}