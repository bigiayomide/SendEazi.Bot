// Bot.WebApi/Program.cs

using Bot.Host;
using FastEndpoints;

var builder = WebApplication.CreateBuilder(args);

// Add configuration providers (appsettings.json, env vars, etc.)
builder.Configuration.AddJsonFile("appsettings.json", false, true)
    .AddEnvironmentVariables();

// Register all bot services
builder.Services.AddBotServices(builder.Configuration);

// FastEndpoints configuration
builder.Services.AddFastEndpoints();

// Swagger/OpenAPI (optional)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Build the app
var app = builder.Build();

// Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

// FastEndpoints will auto-map your endpoints
app.UseFastEndpoints();

// Fallback for non-API requests (if needed)
app.MapFallbackToFile("index.html");

app.Run();