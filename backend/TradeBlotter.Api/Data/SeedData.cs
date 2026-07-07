namespace TradeBlotter.Api.Data;

using TradeBlotter.Api.Domain;

// ---------------------------------------------------------------------------
// Expected end-state positions after seeding (verify against positions panel):
//
//   Symbol  Net Qty   Avg Cost   State
//   ------  -------   --------   ------------------------------------------
//   AAPL     350      230.50     LONG   (partial sell; avg cost unchanged)
//   BAC     -200       45.60     SHORT  (long 400 flipped through zero)
//   NVDA     300      140.075    LONG   (partial sell; avg cost unchanged)
//
// This focused three-symbol set still exercises the full position logic:
//   * Weighted-average cost across multiple buys at different prices.
//   * A partial sell that reduces a long while leaving avg cost untouched
//     (AAPL and NVDA).
//   * BAC's oversized sell closes the 400 long and opens a 200 short in one
//     fill -> the position FLIPS THROUGH ZERO and avg cost resets to the fill.
// ---------------------------------------------------------------------------

/// <summary>
/// Seeds the blotter with a deterministic, realistic set of intraday trades for
/// three symbols (AAPL, BAC, NVDA). The data is designed to exercise the
/// position-keeping logic: weighted-average cost, a partial sell, and a
/// long-to-short flip through zero.
/// </summary>
public static class SeedData
{
    /// <summary>
    /// Populates <paramref name="db"/> with the seed trade set. Idempotent:
    /// if any trades already exist the method returns without changes, so it is
    /// safe to call on every startup.
    /// </summary>
    public static void Initialize(BlotterDbContext db)
    {
        // Never double-seed: if the table already has rows, leave it alone.
        if (db.Trades.Any())
        {
            return;
        }

        // Fixed market-open base time (UTC). Every trade gets a strictly
        // increasing, deterministic timestamp so ordering is stable across runs.
        var baseTime = new DateTimeOffset(2026, 7, 7, 13, 30, 0, TimeSpan.Zero);
        var minute = 0;

        // Local factory: assigns a fresh id and the next chronological timestamp.
        Trade Make(string symbol, Side side, decimal quantity, decimal price) => new()
        {
            Id = Guid.NewGuid(),
            Symbol = symbol,
            Side = side,
            Quantity = quantity,
            Price = price,
            Timestamp = baseTime.AddMinutes(minute++),
        };

        // Trades are listed in strict chronological order.
        var trades = new List<Trade>
        {
            // --- Opening buys ---
            Make("AAPL", Side.Buy,  200m, 230.00m),   // AAPL long 200
            Make("NVDA", Side.Buy,  200m, 139.50m),   // NVDA long 200
            Make("BAC",  Side.Buy,  200m,  45.10m),   // BAC  long 200

            // --- Adding at new prices (non-trivial weighted-average cost) ---
            Make("AAPL", Side.Buy,  200m, 231.00m),   // AAPL long 400 @ 230.50
            Make("NVDA", Side.Buy,  100m, 140.30m),   // NVDA long 300
            Make("BAC",  Side.Buy,  200m,  45.30m),   // BAC  long 400 @ 45.20
            Make("NVDA", Side.Buy,  100m, 141.00m),   // NVDA long 400 @ 140.075

            // --- Partial sells: reduce a long, avg cost unchanged ---
            Make("AAPL", Side.Sell,  50m, 235.00m),   // AAPL long 350 @ 230.50
            Make("NVDA", Side.Sell, 100m, 142.00m),   // NVDA long 300 @ 140.075

            // BAC oversized sell: closes the 400 long and opens a 200 short in one
            // fill -> position FLIPS THROUGH ZERO, avg cost resets to the fill price.
            Make("BAC",  Side.Sell, 600m,  45.60m),   // BAC short 200 @ 45.60
        };

        db.Trades.AddRange(trades);
        db.SaveChanges();
    }
}
