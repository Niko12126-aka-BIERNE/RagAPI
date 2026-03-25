namespace RagAPI.Core;

//TODO: This should be moved to appsettings.json and loaded using IOptions<RagConfig>

public class RagConfig
{
    public string LlmModelPath { get; set; } = "./models/llm.gguf";
    public string EmbedModelPath { get; set; } = "./models/embedder.gguf";
    public string QdrantHost { get; set; } = "localhost";
    public int QdrantPort { get; set; } = 6333;
    public int ChunkSize { get; set; } = 400;
    public int ChunkOverlap { get; set; } = 80;
    public int TopK { get; set; } = 4;
    public int LlmGpuLayerCount { get; set; } = 35;
    public int EmbedGpuLayerCount { get; set; } = 35;
    public string SystemPrompt { get; set; } = """
    You are a helpful assistant. Answer using ONLY the context below.
    If the answer is not in the context, say "I couldn't find that in the document."
    """;
    public string SamplingStrategy { get; set; } = "greedy";
    public float Temperature { get; set; } = 0.8f;
    public float TopP { get; set; } = 0.95f;
}