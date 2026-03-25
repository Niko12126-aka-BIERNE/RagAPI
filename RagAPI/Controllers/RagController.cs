using Microsoft.AspNetCore.Mvc;
using RagAPI.Core;

namespace RagAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RagController(RagComponent rag) : ControllerBase
{
    private readonly RagComponent _rag = rag;

    [HttpPost("index")]
    public async Task<IActionResult> Index(IFormFile file)
    {
        if (file.Length == 0)
        {
            return BadRequest("No file provided.");
        }

        var tempPath = Path.Combine(Path.GetTempPath(), file.FileName);

        await using (var stream = System.IO.File.Create(tempPath))
        {
            await file.CopyToAsync(stream);
        }

        var fileName = await _rag.IndexFileAsync(tempPath);

        System.IO.File.Delete(tempPath);

        return Ok(new { filename = fileName });
    }

    [HttpGet("query")]
    public async Task QueryAsync([FromQuery] string question, [FromQuery] string? filename = null)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("X-Accel-Buffering", "no");

        await foreach (var token in _rag.QueryAsync(question, filename))
        {
            await Response.WriteAsync($"data: {token}\n\n");
            await Response.Body.FlushAsync();
        }
    }

    [HttpGet("files")]
    public async Task<IActionResult> ListFiles()
    {
        var files = await _rag.ListFilesAsync();
        return Ok(files);
    }
}