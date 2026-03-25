namespace RagAPI.Middleware;

public class ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
{
    private readonly RequestDelegate _next = next;
    private readonly string _apiKey = configuration["Auth:ApiKey"]
        ?? throw new InvalidOperationException("Auth:ApiKey is missing from configuration.");

    private static readonly string[] ExemptPaths =
    [
        "/health",
        "/swagger",
        "/swagger/index.html",
        "/swagger/v1/swagger.json"
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (IsExempt(path))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue("X-Api-Key", out var providedKey))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("API key is missing.");
            return;
        }

        if (!string.Equals(providedKey, _apiKey, StringComparison.Ordinal))
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync("Invalid API key.");
            return;
        }

        await _next(context);
    }

    private static bool IsExempt(string path)
    {
        return ExemptPaths.Any(exempt => path.StartsWith(exempt, StringComparison.OrdinalIgnoreCase));
    }
}