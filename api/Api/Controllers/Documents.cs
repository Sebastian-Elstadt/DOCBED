using App.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("documents")]
public class DocumentsController(IDocumentPipeline documentPipeline) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> IngestDocumentAsync(IFormFile file)
    {
        using var strm = file.OpenReadStream();
        await documentPipeline.IngestAsync(strm, file.FileName, HttpContext.RequestAborted);
        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> SearchDocumentsAsync(
        [FromQuery(Name = "s")] string searchQuery,
        [FromQuery(Name = "top")] int topK,
        [FromQuery(Name = "docId")] Guid? documentId = null
    )
    {
        var hits = await documentPipeline.SearchAsync(searchQuery, topK, documentId, HttpContext.RequestAborted);
        return Ok(hits);
    }
}