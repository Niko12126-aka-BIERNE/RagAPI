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

// ------------ Auto-start Qdrant via Docker Compose ------------ //
var compose = new System.Diagnostics.ProcessStartInfo
{
    FileName = "docker",
    Arguments = "compose up -d",
    WorkingDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../..")),
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    UseShellExecute = false
};

using var process = System.Diagnostics.Process.Start(compose);
await process!.WaitForExitAsync();
// ------------------------------------------------------------- //

var app = builder.Build();

// Initialize RagComponent to load models and establish Qdrant connection at startup
app.Services.GetRequiredService<RagComponent>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();