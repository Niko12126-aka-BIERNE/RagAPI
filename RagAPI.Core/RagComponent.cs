using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using RagAPI.Core.Providers;

namespace RagAPI.Core;

public class RagComponent : IDisposable
{
    private readonly RagConfig _config;
    private readonly ILlmProvider _llm;
    private readonly HttpClient _embedClient;
    private readonly QdrantClient _qdrant;

    public RagComponent(RagConfig config)
    {
        _config = config;
        _llm = LlmProviderFactory.Create(config);
        _embedClient = new HttpClient { BaseAddress = new Uri(config.EmbedApiUrl) };
        _qdrant = new QdrantClient(config.QdrantHost, config.QdrantPort, https: false);

        InitialiseCollectionAsync().GetAwaiter().GetResult();
    }

    private async Task InitialiseCollectionAsync()
    {
        var testVector = await EmbedAsync("test");
        var vectorSize = (ulong)testVector.Length;

        var collections = await _qdrant.ListCollectionsAsync();
        if (!collections.Any(c => c == _config.CollectionName))
        {
            await _qdrant.CreateCollectionAsync(_config.CollectionName,
                new VectorParams
                {
                    Size = vectorSize,
                    Distance = Distance.Cosine
                });
        }
    }

    private async Task<float[]> EmbedAsync(string text)
    {
        var request = new { model = _config.EmbedModelName, input = text };
        var response = await _embedClient.PostAsJsonAsync("/v1/embeddings", request);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Embedding failed ({response.StatusCode}): {errorBody}");
        }

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var embedding = doc.RootElement
            .GetProperty("data")[0]
            .GetProperty("embedding")
            .EnumerateArray()
            .Select(e => e.GetSingle())
            .ToArray();

        return embedding;
    }

    public async Task<string> IndexFileAsync(string filePath)
    {
        var text = FileReader.Read(filePath);
        var chunks = TextChunker.Chunk(text, _config.ChunkSize, _config.ChunkOverlap);
        var fileName = Path.GetFileNameWithoutExtension(filePath).ToLowerInvariant();

        Console.WriteLine($"Embedding {chunks.Count} chunks from {fileName}...");

        var points = new List<PointStruct>();
        foreach (var chunk in chunks)
        {
            var vector = await EmbedAsync(chunk);
            points.Add(new PointStruct
            {
                Id = (ulong)Guid.NewGuid().GetHashCode(),
                Vectors = vector,
                Payload =
                {
                    ["text"] = chunk,
                    ["filename"] = fileName
                }
            });
        }

        await _qdrant.UpsertAsync(_config.CollectionName, points);

        return fileName;
    }

    public async IAsyncEnumerable<string> QueryAsync(
        string question,
        string? filename = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var qVector = await EmbedAsync(question);

        Filter? filter = filename is not null
            ? new Filter
            {
                Must =
                {
                    new Condition
                    {
                        Field = new FieldCondition
                        {
                            Key = "filename",
                            Match = new Match { Text = filename }
                        }
                    }
                }
            }
            : null;

        var results = await _qdrant.SearchAsync(
            _config.CollectionName,
            qVector,
            filter: filter,
            limit: (ulong)_config.TopK,
            cancellationToken: ct
        );

        var context = string.Join("\n\n", results.Select((r, i) =>
            $"[{i + 1}] {r.Payload["text"].StringValue}"));

        var userMessage = $"""
            --- CONTEXT ---
            {context}
            --- END CONTEXT ---

            Question: {question}
            """;

        await foreach (var token in _llm.GenerateAsync(_config.SystemPrompt, userMessage, ct).WithCancellation(ct))
        {
            yield return token;
        }
    }

    public async Task<IEnumerable<string>> ListFilesAsync()
    {
        var result = await _qdrant.ScrollAsync(
            _config.CollectionName,
            limit: 1000,
            payloadSelector: true
        );

        return result.Result
            .Select(p => p.Payload["filename"].StringValue)
            .Distinct()
            .OrderBy(f => f);
    }

    public void Dispose()
    {
        _embedClient.Dispose();
        _qdrant.Dispose();
        GC.SuppressFinalize(this);
    }
}