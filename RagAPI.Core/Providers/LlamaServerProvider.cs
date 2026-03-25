using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace RagAPI.Core.Providers;

public class LlamaServerProvider : ILlmProvider
{
    private readonly HttpClient _http;
    private readonly HttpClient _visionHttp;
    private readonly string _model;

    public LlamaServerProvider(string url, string model, string visionUrl)
    {
        _http = new HttpClient { BaseAddress = new Uri(url) };
        _visionHttp = new HttpClient
        {
            BaseAddress = new Uri(visionUrl),
            Timeout = TimeSpan.FromMinutes(10)
        };
        _model = model;
    }

    public async IAsyncEnumerable<string> GenerateAsync(
        string systemPrompt,
        string userMessage,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var request = new
        {
            model = _model,
            stream = true,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user",   content = userMessage  }
            }
        };

        var response = await _http.PostAsJsonAsync("/v1/chat/completions", request, ct);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (await reader.ReadLineAsync(ct) is { } line)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (!line.StartsWith("data: ")) continue;
            if (line == "data: [DONE]") break;

            var json = line["data: ".Length..];
            var doc = JsonDocument.Parse(json);
            var delta = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("delta");

            if (delta.TryGetProperty("content", out var contentProp))
            {
                var content = contentProp.GetString();
                if (!string.IsNullOrEmpty(content))
                {
                    yield return content;
                }
            }
        }
    }

    public async Task<string> DescribeImageAsync(
        string imagePath,
        string prompt,
        CancellationToken ct = default)
    {
        var imageBytes = await File.ReadAllBytesAsync(imagePath, ct);
        var base64Image = Convert.ToBase64String(imageBytes);
        var ext = Path.GetExtension(imagePath).TrimStart('.').ToLower();
        var mimeType = ext == "jpg" || ext == "jpeg" ? "image/jpeg" : $"image/{ext}";

        var request = new
        {
            model = "qwen2.5-vl",
            stream = false,
            messages = new[]
            {
                new
                {
                    role    = "user",
                    content = new object[]
                    {
                        new { type = "text",       text      = prompt },
                        new { type = "image_url",  image_url = new { url = $"data:{mimeType};base64,{base64Image}" } }
                    }
                }
            }
        };

        var response = await _visionHttp.PostAsJsonAsync("/v1/chat/completions", request, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        var doc = JsonDocument.Parse(json);
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return content ?? string.Empty;
    }
}