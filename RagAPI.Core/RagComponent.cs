using LLama;
using LLama.Common;
using LLama.Sampling;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace RagAPI.Core;

public class RagComponent : IDisposable
{
    private readonly RagConfig _config;
    private readonly LLamaWeights _llmWeights;
    private readonly LLamaWeights _embedWeights;
    private readonly LLamaEmbedder _embedder;
    private readonly QdrantClient _qdrant;

    public RagComponent(RagConfig config)
    {
        _config = config;

        var llmParams = new ModelParams(config.LlmModelPath)
        {
            ContextSize = 4096,
            GpuLayerCount = config.LlmGpuLayerCount
        };

        var embedParams = new ModelParams(config.EmbedModelPath)
        {
            ContextSize = 512,
            GpuLayerCount = config.EmbedGpuLayerCount
        };

        _llmWeights = LLamaWeights.LoadFromFile(llmParams);
        _embedWeights = LLamaWeights.LoadFromFile(embedParams);
        _embedder = new LLamaEmbedder(_embedWeights, embedParams);

        _qdrant = new QdrantClient(config.QdrantHost, config.QdrantPort, https: false);
    }

    public async Task<string> IndexFileAsync(string filePath)
    {
        var text = FileReader.Read(filePath);
        var chunks = TextChunker.Chunk(text, _config.ChunkSize, _config.ChunkOverlap);

        var vectors = new List<float[]>();
        foreach (var chunk in chunks)
        {
            var embedding = await _embedder.GetEmbeddings(chunk);
            vectors.Add(embedding[0]);
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

    public async IAsyncEnumerable<string> QueryAsync(string question, string collectionName)
    {
        var qEmbeddings = await _embedder.GetEmbeddings(question);
        var qVector = qEmbeddings[0];

        var results = await _qdrant.SearchAsync(
            collectionName,
            qVector,
            limit: (ulong)_config.TopK
        );

        var context = string.Join("\n\n", results.Select((r, i) =>
            $"[{i + 1}] {r.Payload["text"].StringValue}"));

        var history = new ChatHistory();
        history.AddMessage(AuthorRole.System, _config.SystemPrompt);
        history.AddMessage(AuthorRole.User, $"""
            --- CONTEXT ---
            {context}
            --- END CONTEXT ---
            """);
        history.AddMessage(AuthorRole.Assistant, "I have read the context. I will now answer the question.");

        var context2 = _llmWeights.CreateContext(
            new ModelParams(_config.LlmModelPath) { ContextSize = 4096, GpuLayerCount = _config.LlmGpuLayerCount });

        var executor = new InteractiveExecutor(context2);
        var session = new ChatSession(executor, history);

        await foreach (var token in session.ChatAsync(
            new ChatHistory.Message(AuthorRole.User, question),
            new InferenceParams
            {
                MaxTokens = 512,
                AntiPrompts = new List<string> { "User:", "<|im_end|>", "<|end|>" },
                SamplingPipeline = BuildSamplingPipeline()
            }))
        {
            yield return token;
        }

        context2.Dispose();
    }

    private ISamplingPipeline BuildSamplingPipeline()
    {
        return _config.SamplingStrategy.ToLower() switch
        {
            "greedy" => new GreedySamplingPipeline(),
            "temperature" => new DefaultSamplingPipeline { Temperature = _config.Temperature },
            "topp" => new DefaultSamplingPipeline { TopP = _config.TopP },
            _ => throw new NotSupportedException($"Unknown sampling strategy: {_config.SamplingStrategy}")
        };
    }

    public async Task<IEnumerable<string>> ListCollectionsAsync()
    {
        return await _qdrant.ListCollectionsAsync();
    }

    public void Dispose()
    {
        _embedder.Dispose();
        _embedWeights.Dispose();
        _llmWeights.Dispose();
        GC.SuppressFinalize(this);
    }
}