using System.Net;
using Infra.DocumentConverter;
using Microsoft.Extensions.Logging.Abstractions;

namespace Infra.UnitTests;

public class PythonDocumentConverterServiceTests
{
    private static PythonDocumentConverterService Build(string ndjsonResponseBody)
    {
        var handler = new StubHttpMessageHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(ndjsonResponseBody, System.Text.Encoding.UTF8, "application/x-ndjson")
        }));
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://doc-converter:8000/") };
        return new PythonDocumentConverterService(http, NullLogger<PythonDocumentConverterService>.Instance);
    }

    [Fact]
    public async Task ConvertToPageImagesAsync_ParsesNdjsonStreamWithSnakeCase()
    {
        var body = string.Join('\n', new[]
        {
            """{"success":true,"page":1,"image_base64":"AAA","format":"jpeg"}""",
            """{"success":true,"page":2,"image_base64":"BBB","format":"jpeg"}"""
        });

        var svc = Build(body);
        using var stream = new MemoryStream();

        var pages = new List<App.DocumentConverter.DocumentPageConversionResult>();
        await foreach (var page in svc.ConvertToPageImagesAsync(stream, "doc.pdf"))
            pages.Add(page);

        Assert.Equal(2, pages.Count);
        Assert.True(pages[0].Success);
        Assert.Equal(1, pages[0].Page);
        Assert.Equal("AAA", pages[0].ImageBase64);
        Assert.Equal("jpeg", pages[0].Format);
        Assert.Equal(2, pages[1].Page);
        Assert.Equal("BBB", pages[1].ImageBase64);
    }

    [Fact]
    public async Task ConvertToPageImagesAsync_SkipsBlankLines()
    {
        var body = "\n" + """{"success":true,"page":1,"image_base64":"AAA","format":"jpeg"}""" + "\n\n";
        var svc = Build(body);

        var pages = new List<App.DocumentConverter.DocumentPageConversionResult>();
        await foreach (var page in svc.ConvertToPageImagesAsync(new MemoryStream(), "doc.pdf"))
            pages.Add(page);

        Assert.Single(pages);
    }

    [Fact]
    public async Task ConvertToPageImagesAsync_PassesThroughErrorPagesFromConverter()
    {
        var body = """{"success":false,"page":3,"image_base64":null,"format":null,"error":"boom"}""";
        var svc = Build(body);

        var pages = new List<App.DocumentConverter.DocumentPageConversionResult>();
        await foreach (var page in svc.ConvertToPageImagesAsync(new MemoryStream(), "doc.pdf"))
            pages.Add(page);

        Assert.Single(pages);
        Assert.False(pages[0].Success);
        Assert.Equal("boom", pages[0].Error);
    }
}
