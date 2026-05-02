using App.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("documents")]
public class DocumentsController(IDocumentVisionService docVisionService) : ControllerBase
{
    [HttpPost("analysis")]
    public async Task<IActionResult> Analyze(IFormFile file)
    {
        using var strm = file.OpenReadStream();
        var result = await docVisionService.AnalyzeDocumentAsync(strm, file.FileName, new(0.2, 8192), HttpContext.RequestAborted);
        return Content(result, "application/json");
    }
}