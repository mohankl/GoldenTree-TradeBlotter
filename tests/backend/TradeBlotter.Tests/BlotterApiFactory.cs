using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TradeBlotter.Api.Data;

namespace TradeBlotter.Tests;

/// <summary>
/// Boots the real API for integration tests, but swaps the SQLite connection for a
/// dedicated, private in-memory database so each factory instance is isolated and
/// nothing touches the developer's blotter.db file.
/// </summary>
public class BlotterApiFactory : WebApplicationFactory<Program>
{
    // A shared open connection keeps the in-memory database alive for the app's lifetime.
    private readonly Microsoft.Data.Sqlite.SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        // Keep the test database empty and deterministic — no demo seeding.
        builder.UseSetting("SeedData", "false");

        builder.ConfigureServices(services =>
        {
            // Remove the app's registered DbContext options and re-register on our connection.
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<BlotterDbContext>));
            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<BlotterDbContext>(options => options.UseSqlite(_connection));

            // Create the schema on our in-memory connection.
            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BlotterDbContext>();
            db.Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
