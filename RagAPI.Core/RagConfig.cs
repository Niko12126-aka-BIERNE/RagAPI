namespace RagAPI.Core;

public class RagConfig
{
    // Embedding — always local via llama-server
    public string EmbedApiUrl { get; set; } = "http://localhost:8081";
    public string EmbedModelName { get; set; } = "text-embedding";

    // LLM provider selection
    public string LlmProvider { get; set; } = "llamaserver";

    // llama-server (local)
    public string LlamaServerUrl { get; set; } = "http://localhost:8080";
    public string LlamaModelName { get; set; } = "qwen3";

    // Anthropic (Claude)
    public string AnthropicApiKey { get; set; } = "";
    public string AnthropicModel { get; set; } = "claude-sonnet-4-5";

    // Qdrant
    public string QdrantHost { get; set; } = "localhost";
    public int QdrantPort { get; set; } = 6334;

    // RAG settings
    public int ChunkSize { get; set; } = 400;
    public int ChunkOverlap { get; set; } = 80;
    public int TopK { get; set; } = 4;
    public string CollectionName { get; set; } = "documents";
    public string SystemPrompt { get; set; } = "You are a helpful assistant. Answer using ONLY the context provided. If the answer is not in the context, say 'I couldn't find that in the document.'";
}