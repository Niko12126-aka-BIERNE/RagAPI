namespace RagAPI.Core.Providers;

public static class LlmProviderFactory
{
    public static ILlmProvider Create(RagConfig config) => config.LlmProvider.ToLower() switch
    {
        "llamaserver" => new LlamaServerProvider(config.LlamaServerUrl, config.LlamaModelName, config.VisionServerUrl),
        "anthropic" => new AnthropicProvider(config.AnthropicApiKey, config.AnthropicModel),
        _ => throw new NotSupportedException($"Unknown LLM provider: {config.LlmProvider}. Supported: llamaserver, anthropic")
    };
}