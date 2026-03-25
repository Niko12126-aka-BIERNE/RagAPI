using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace RagAPI.Core.Providers;

public class AnthropicProvider : ILlmProvider
{
    private readonly HttpClient _http;
    private readonly string _model;

    public AnthropicProvider(string apiKey, string model)
    {
        _model = model;
        _http = new HttpClient { BaseAddress = new Uri("https://api.anthropic.com") };
        _http.DefaultRequestHeaders.Add("x-api-key", apiKey);
        _http.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
    }

    public async IAsyncEnumerable<string> GenerateAsync(
        string systemPrompt,
        string userMessage,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var request = new
        {
            model = _model,
            max_tokens = 1024,
            stream = true,
            system = systemPrompt,
            messages = new[]
            {
                new { role = "user", content = userMessage }
            }
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _http.PostAsync("/v1/messages", content, ct);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (await reader.ReadLineAsync(ct) is { } line)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (!line.StartsWith("data: ")) continue;

            var data = line["data: ".Length..];
            var doc = JsonDocument.Parse(data);

            if (!doc.RootElement.TryGetProperty("type", out var type)) continue;

            if (type.GetString() == "content_block_delta")
            {
                var delta = doc.RootElement
                    .GetProperty("delta")
                    .GetProperty("text")
                    .GetString();

                if (!string.IsNullOrEmpty(delta))
                {
                    yield return delta;
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
        var mediaType = ext == "jpg" || ext == "jpeg" ? "image/jpeg" : $"image/{ext}";

        var request = new
        {
            model = _model,
            max_tokens = 1024,
            messages = new[]
            {
                new
                {
                    role    = "user",
                    content = new object[]
                    {
                        new
                        {
                            type   = "image",
                            source = new
                            {
                                type         = "base64",
                                media_type   = mediaType,
                                data         = base64Image
                            }
                        },
                        new { type = "text", text = prompt }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _http.PostAsync("/v1/messages", content, ct);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(ct);
        var doc = JsonDocument.Parse(responseJson);
        var text = doc.RootElement
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString();

        return text ?? string.Empty;
    }
}