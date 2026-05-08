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
}