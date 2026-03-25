namespace RagAPI.Core.Providers;

public interface ILlmProvider
{
    IAsyncEnumerable<string> GenerateAsync(string systemPrompt, string userMessage, CancellationToken ct = default);
}