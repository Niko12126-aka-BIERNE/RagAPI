using Microsoft.AspNetCore.Mvc;
using RagAPI.Core;

namespace RagAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RagController(RagComponent rag, ILogger<RagController> logger) : ControllerBase
{
    private readonly RagComponent _rag = rag;
    private readonly ILogger<RagController> _logger = logger;

    [HttpPost("index")]
    public async Task<IActionResult> Index(IFormFile file)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("No file provided.");
        }

        var allowedExtensions = new[] { ".txt", ".md", ".pdf", ".docx" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(ext))
        {
            return BadRequest($"Unsupported file type: {ext}. Supported: {string.Join(", ", allowedExtensions)}");
        }

        var tempPath = Path.Combine(Path.GetTempPath(), file.FileName);

        try
        {
            await using (var stream = System.IO.File.Create(tempPath))
            {
                await file.CopyToAsync(stream);
            }

            var fileName = await _rag.IndexFileAsync(tempPath);
            return Ok(new { filename = fileName });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to index file {FileName}", file.FileName);
            return StatusCode(500, "An error occurred while indexing the file.");
        }
        finally
        {
            if (System.IO.File.Exists(tempPath))
            {
                System.IO.File.Delete(tempPath);
            }
        }
    }

    [HttpGet("query")]
    public async Task QueryAsync([FromQuery] string question, [FromQuery] string? filename = null)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            Response.StatusCode = 400;
            await Response.WriteAsync("Question cannot be empty.");
            return;
        }

        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("X-Accel-Buffering", "no");

        try
        {
            await foreach (var token in _rag.QueryAsync(question, filename))
            {
                await Response.WriteAsync($"data: {token}\n\n");
                await Response.Body.FlushAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process query: {Question}", question);
            await Response.WriteAsync("data: [ERROR] An error occurred while processing your query.\n\n");
            await Response.Body.FlushAsync();
        }
    }

    [HttpGet("files")]
    public async Task<IActionResult> ListFiles()
    {
        try
        {
            var files = await _rag.ListFilesAsync();
            return Ok(files);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list files");
            return StatusCode(500, "An error occurred while retrieving the file list.");
        }
    }
}