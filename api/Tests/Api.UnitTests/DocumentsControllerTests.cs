using System.Text;
using Api.Controllers;
using App.Abstractions;
using App.VectorStore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Api.UnitTests;

public class DocumentsControllerTests
{
    [Fact]
    public async Task IngestDocumentAsync_ForwardsFileAndNameToPipelineAndReturnsOk()
    {
        var pipeline = Substitute.For<IDocumentPipeline>();
        var controller = new DocumentsController(pipeline)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var bytes = Encoding.UTF8.GetBytes("hello pdf");
        using var ms = new MemoryStream(bytes);
        var formFile = new FormFile(ms, 0, bytes.Length, "file", "doc.pdf");

        var result = await controller.IngestDocumentAsync(formFile);

        Assert.IsType<OkResult>(result);
        await pipeline.Received(1).IngestAsync(
            Arg.Any<Stream>(),
            "doc.pdf",
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task IngestDocumentAsync_PassesRequestAbortedToken()
    {
        var pipeline = Substitute.For<IDocumentPipeline>();
        using var cts = new CancellationTokenSource();
        var httpContext = new DefaultHttpContext { RequestAborted = cts.Token };
        var controller = new DocumentsController(pipeline)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var formFile = new FormFile(new MemoryStream(new byte[] { 1, 2, 3 }), 0, 3, "file", "x.pdf");

        await controller.IngestDocumentAsync(formFile);

        await pipeline.Received(1).IngestAsync(Arg.Any<Stream>(), Arg.Any<string>(), cts.Token);
    }
}
