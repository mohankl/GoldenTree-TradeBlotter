using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using TradeBlotter.Api.Data;
using TradeBlotter.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

const string CorsPolicy = "spa";

// In a container (e.g. Railway) bind Kestrel to 0.0.0.0 on the injected PORT.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// Serialize enums as strings (e.g. "Buy"/"Sell") for a friendly, self-describing contract.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// EF Core + SQLite. Connection string is configurable so tests can point at a temp DB.
var connectionString = builder.Configuration.GetConnectionString("Blotter")
    ?? "Data Source=blotter.db";
builder.Services.AddDbContext<BlotterDbContext>(options => options.UseSqlite(connectionString));

// CORS: allow the Vite dev server by default, plus any origins supplied via the
// "AllowedOrigins" config (comma-separated) for the deployed SPA. A value of "*"
// allows any origin (used for the public demo deployment).
var allowedOrigins = (builder.Configuration["AllowedOrigins"] ?? "")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
    {
        policy.AllowAnyHeader().AllowAnyMethod();
        if (allowedOrigins.Contains("*"))
        {
            policy.AllowAnyOrigin();
        }
        else
        {
            policy.WithOrigins(
                ["http://localhost:5173", "http://127.0.0.1:5173", .. allowedOrigins]);
        }
    });
});

// RFC 7807 problem-details for error responses.
builder.Services.AddProblemDetails();

var app = builder.Build();

// Map exceptions to problem-details. A malformed body / wrong type throws
// BadHttpRequestException (which carries a 400); everything else is a 500.
app.UseExceptionHandler(handler =>
{
    handler.Run(async context =>
    {
        var error = context.Features
            .Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;

        var status = error is BadHttpRequestException bad
            ? bad.StatusCode
            : StatusCodes.Status500InternalServerError;

        await Results.Problem(
            statusCode: status,
            title: status == StatusCodes.Status400BadRequest
                ? "The request body is invalid."
                : "An unexpected error occurred.")
            .ExecuteAsync(context);
    });
});
app.UseStatusCodePages();
app.UseCors(CorsPolicy);

// Ensure the database exists on startup (simple schema; no migrations needed here).
// Seed demo data (banks + tech) on first run unless disabled — the test host turns
// this off via the "SeedData" setting so integration tests see an empty database.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BlotterDbContext>();
    db.Database.EnsureCreated();

    if (app.Configuration.GetValue("SeedData", true))
    {
        SeedData.Initialize(db);
    }
}

app.MapTradeEndpoints();

app.Run();

// Exposed so the test project's WebApplicationFactory<Program> can boot the real app.
public partial class Program;
