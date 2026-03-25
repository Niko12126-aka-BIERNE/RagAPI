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
    }

    private async Task<float[]> EmbedAsync(string text)
    {
        var request = new { model = _config.EmbedModelName, input = text };
        var response = await _embedClient.PostAsJsonAsync("/v1/embeddings", request);
        response.EnsureSuccessStatusCode();

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

        Console.WriteLine($"Embedding {chunks.Count} chunks...");
        var vectors = new List<float[]>();
        foreach (var chunk in chunks)
        {
            vectors.Add(await EmbedAsync(chunk));
        }

        var vectorSize = (ulong)vectors[0].Length;

        var collectionName = Path.GetFileNameWithoutExtension(filePath)
                                 .ToLowerInvariant()
                                 .Replace(" ", "_");

        var collections = await _qdrant.ListCollectionsAsync();
        if (!collections.Any(c => c == collectionName))
        {
            await _qdrant.CreateCollectionAsync(collectionName,
                new VectorParams
                {
                    Size = vectorSize,
                    Distance = Distance.Cosine
                });
        }

        var points = chunks.Select((chunk, i) => new PointStruct
        {
            Id = (ulong)i,
            Vectors = vectors[i],
            Payload = { ["text"] = chunk }
        }).ToList();

        await _qdrant.UpsertAsync(collectionName, points);

        return collectionName;
    }

    public async IAsyncEnumerable<string> QueryAsync(
        string question,
        string collectionName,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var qVector = await EmbedAsync(question);

        var results = await _qdrant.SearchAsync(collectionName, qVector, limit: (ulong)_config.TopK, cancellationToken: ct);

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

    public async Task<IEnumerable<string>> ListCollectionsAsync()
    {
        return await _qdrant.ListCollectionsAsync();
    }

    public void Dispose()
    {
        _embedClient.Dispose();
        _qdrant.Dispose();
        GC.SuppressFinalize(this);
    }
}