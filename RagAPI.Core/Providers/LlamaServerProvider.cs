using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace RagAPI.Core.Providers;

public class LlamaServerProvider : ILlmProvider
{
    private readonly HttpClient _http;
    private readonly string _model;

    public LlamaServerProvider(string url, string model)
    {
        _http = new HttpClient { BaseAddress = new Uri(url) };
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
                new { role = "user", content = userMessage }
            }
        };

        var response = await _http.PostAsJsonAsync("/v1/chat/completions", request, ct);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (await reader.ReadLineAsync(ct) is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (!line.StartsWith("data: "))
            {
                continue;
            }

            if (line == "data: [DONE]")
            {
                break;
            }

            var json = line["data: ".Length..];
            var doc = JsonDocument.Parse(json);
            var delta = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("delta");

            if (delta.TryGetProperty("content", out var contentProp))
            {
                var content = contentProp.GetString();
                if (!string.IsNullOrEmpty(content))
                    yield return content;
            }
        }
    }
}