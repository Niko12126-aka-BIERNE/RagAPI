namespace RagAPI.Core.Providers;

public interface ILlmProvider
{
    IAsyncEnumerable<string> GenerateAsync(string systemPrompt, string userMessage, CancellationToken ct = default);
    Task<string> DescribeImageAsync(string imagePath, string prompt, CancellationToken ct = default);
}