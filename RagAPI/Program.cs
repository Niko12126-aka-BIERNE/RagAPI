using RagAPI.Core;
using RagAPI.Middleware;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Name = "X-Api-Key",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Description = "Enter your API key"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configure CORS using allowed origins from appsettings.json
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()
    ?? throw new InvalidOperationException("Cors:AllowedOrigins configuration is missing from appsettings.json");

builder.Services.AddCors(options =>
{
    options.AddPolicy("RagPolicy", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Load RagConfig from appsettings.json
var ragConfig = builder.Configuration
    .GetSection("RagConfigurations")
    .Get<RagConfig>()
    ?? throw new InvalidOperationException("Rag configuration section is missing from appsettings.json");

// Register RagConfig and RagComponent as singletons
builder.Services.AddSingleton(ragConfig);
builder.Services.AddSingleton<RagComponent>();

// Add health checks for monitoring
builder.Services.AddHealthChecks();

var app = builder.Build();

// Initialize RagComponent to load models and establish Qdrant connection at startup
app.Services.GetRequiredService<RagComponent>();

app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<ApiKeyMiddleware>();
app.UseCors("RagPolicy");

app.UseAuthorization();
app.MapControllers();

app.MapHealthChecks("/health");

app.Run();