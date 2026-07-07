using Microsoft.EntityFrameworkCore;
using TradeBlotter.Api.Data;
using TradeBlotter.Api.Domain;
using TradeBlotter.Api.Dtos;

namespace TradeBlotter.Api.Endpoints;

/// <summary>Endpoint definitions for trades and derived positions.</summary>
public static class TradeEndpoints
{
    public static void MapTradeEndpoints(this IEndpointRouteBuilder app)
    {
        // POST /trades — submit a new trade.
        app.MapPost("/trades", async (TradeRequest request, BlotterDbContext db) =>
        {
            var errors = Validate(request, out var side);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var trade = new Trade
            {
                Id = Guid.NewGuid(),
                Symbol = request.Symbol!.Trim().ToUpperInvariant(),
                Side = side,
                Quantity = request.Quantity!.Value,
                Price = request.Price!.Value,
                Timestamp = DateTimeOffset.UtcNow
            };

            db.Trades.Add(trade);
            await db.SaveChangesAsync();

            var response = TradeResponse.From(trade);
            return Results.Created($"/trades/{trade.Id}", response);
        });

        // GET /trades — all trades, newest first.
        app.MapGet("/trades", async (BlotterDbContext db) =>
        {
            // SQLite cannot ORDER BY a DateTimeOffset, so sort newest-first in memory.
            // Trade volumes here are small; this keeps the timestamp type clean.
            var trades = await db.Trades.ToListAsync();
            var ordered = trades.OrderByDescending(t => t.Timestamp).Select(TradeResponse.From);
            return Results.Ok(ordered);
        });

        // GET /positions — derived positions per symbol (zero-net omitted).
        app.MapGet("/positions", async (BlotterDbContext db) =>
        {
            var trades = await db.Trades.ToListAsync();
            var positions = PositionCalculator.Derive(trades);
            return Results.Ok(positions.Select(PositionResponse.From));
        });
    }

    /// <summary>
    /// Validates a trade submission. Returns a field → messages map suitable for
    /// <see cref="Results.ValidationProblem"/> (RFC 7807 problem-details, HTTP 400).
    /// </summary>
    private static Dictionary<string, string[]> Validate(TradeRequest request, out Side side)
    {
        var errors = new Dictionary<string, string[]>();
        side = default;

        if (string.IsNullOrWhiteSpace(request.Symbol))
        {
            errors["symbol"] = ["Symbol is required."];
        }

        // Enum.TryParse also accepts numeric strings (e.g. "123") and undefined
        // values, so require the parsed result to be a defined Buy/Sell member.
        if (string.IsNullOrWhiteSpace(request.Side)
            || !Enum.TryParse(request.Side, ignoreCase: true, out side)
            || !Enum.IsDefined(side))
        {
            errors["side"] = ["Side is required and must be 'Buy' or 'Sell'."];
        }

        if (request.Quantity is null || request.Quantity <= 0)
        {
            errors["quantity"] = ["Quantity must be a positive number."];
        }

        if (request.Price is null || request.Price <= 0)
        {
            errors["price"] = ["Price must be a positive number."];
        }

        return errors;
    }
}
