using RagAPI.Core;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Load RagConfig from appsettings.json
var ragConfig = builder.Configuration
    .GetSection("RagConfigurations")
    .Get<RagConfig>()
    ?? throw new InvalidOperationException("Rag configuration section is missing from appsettings.json");

// Register RagConfig and RagComponent as singletons
builder.Services.AddSingleton(ragConfig);
builder.Services.AddSingleton<RagComponent>();

var app = builder.Build();

// Initialize RagComponent to load models and establish Qdrant connection at startup
app.Services.GetRequiredService<RagComponent>();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();
app.MapControllers();

app.Run();