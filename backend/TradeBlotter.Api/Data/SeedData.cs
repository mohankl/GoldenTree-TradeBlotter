namespace TradeBlotter.Api.Data;

using TradeBlotter.Api.Domain;

// ---------------------------------------------------------------------------
// Expected end-state positions after seeding (verify against positions panel):
//
//   Symbol  Net Qty   Avg Cost    State
//   ------  -------   --------    -----------------------------------------
//   WFC       0       -           FLAT   (fully closed -> omitted from positions)
//   BAC     -200      45.60       SHORT  (long 400 flipped through zero to short)
//   JPM      200      250.657     LONG   (partial sell, avg cost unchanged)
//   GOOGL    300      185.317     LONG
//   MSFT     150      460.24      LONG   (partial sell, avg cost unchanged)
//   AAPL     350      230.229     LONG
//   NVDA     300      140.075     LONG   (partial sell, avg cost unchanged)
//
// Notes for the reviewer:
//   * WFC ends net-zero  -> exercises the "closed position is hidden" path.
//   * BAC flips long->short in a single oversized sell -> exercises the
//     average-cost reset when a position crosses through zero.
//   * JPM / MSFT / NVDA each have a partial sell that reduces a long while
//     leaving weighted-average cost untouched.
// ---------------------------------------------------------------------------

/// <summary>
/// Seeds the blotter with a deterministic, realistic set of intraday trades.
/// The data is designed to exercise the position-keeping logic: weighted-average
/// cost, partial sells, a long-to-short flip through zero, and a fully closed
/// (net-zero) symbol.
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

        // Trades are listed in strict chronological order. Within each symbol the
        // relative order is preserved, which is all the position logic depends on.
        var trades = new List<Trade>
        {
            // --- Opening burst ---
            Make("WFC",   Side.Buy,  100m,  70.10m),   // WFC long 100
            Make("AAPL",  Side.Buy,  150m, 229.50m),   // AAPL long 150
            Make("NVDA",  Side.Buy,  200m, 139.50m),   // NVDA long 200
            Make("BAC",   Side.Buy,  200m,  45.10m),   // BAC long 200
            Make("MSFT",  Side.Buy,  100m, 459.00m),   // MSFT long 100
            Make("GOOGL", Side.Buy,  100m, 184.50m),   // GOOGL long 100
            Make("JPM",   Side.Buy,  100m, 250.20m),   // JPM long 100

            // --- Adding to positions at new prices (non-trivial avg cost) ---
            Make("WFC",   Side.Buy,  100m,  70.40m),   // WFC long 200 @ 70.25
            Make("AAPL",  Side.Buy,  100m, 230.80m),   // AAPL long 250
            Make("NVDA",  Side.Buy,  100m, 140.30m),   // NVDA long 300
            Make("BAC",   Side.Buy,  100m,  45.30m),   // BAC long 300
            Make("MSFT",  Side.Buy,  100m, 461.50m),   // MSFT long 200
            Make("GOOGL", Side.Buy,  100m, 185.80m),   // GOOGL long 200
            Make("JPM",   Side.Buy,  150m, 251.00m),   // JPM long 250

            // --- Mid-session activity ---
            Make("WFC",   Side.Sell, 100m,  70.85m),   // WFC partial sell -> long 100
            Make("BAC",   Side.Buy,  100m,  45.20m),   // BAC long 400 @ 45.175
            Make("NVDA",  Side.Buy,  100m, 141.00m),   // NVDA long 400 @ 140.075
            Make("GOOGL", Side.Buy,   50m, 186.20m),   // GOOGL long 250
            Make("MSFT",  Side.Buy,   50m, 460.20m),   // MSFT long 250 @ 460.24
            Make("JPM",   Side.Buy,  100m, 250.60m),   // JPM long 350 @ 250.657
            Make("AAPL",  Side.Buy,   50m, 231.40m),   // AAPL long 300

            // --- Late-session unwinds ---
            Make("WFC",   Side.Sell, 100m,  71.05m),   // WFC net-zero CLOSE -> flat (hidden from positions)
            Make("NVDA",  Side.Sell, 100m, 142.00m),   // NVDA partial sell -> long 300, avg unchanged
            Make("MSFT",  Side.Sell, 100m, 463.00m),   // MSFT partial sell -> long 150, avg unchanged
            Make("JPM",   Side.Sell, 150m, 252.30m),   // JPM partial sell -> long 200, avg unchanged
            Make("GOOGL", Side.Buy,   50m, 185.10m),   // GOOGL long 300 @ 185.317
            Make("AAPL",  Side.Buy,   50m, 230.10m),   // AAPL long 350 @ 230.229

            // BAC oversized sell: closes the 400 long and opens a 200 short in one
            // fill -> position FLIPS THROUGH ZERO, avg cost resets to the fill price.
            Make("BAC",   Side.Sell, 600m,  45.60m),   // BAC short 200 @ 45.60
        };

        db.Trades.AddRange(trades);
        db.SaveChanges();
    }
}
