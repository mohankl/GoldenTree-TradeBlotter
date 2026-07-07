using TradeBlotter.Api.Domain;

namespace TradeBlotter.Tests;

/// <summary>
/// Unit tests for the critical position/average-cost logic. These double-check the
/// four rules (open / increase-weighted-avg / reduce-unchanged / flip-reset).
/// </summary>
public class PositionCalculatorTests
{
    private static Trade T(string symbol, Side side, decimal qty, decimal price, int minute) => new()
    {
        Id = Guid.NewGuid(),
        Symbol = symbol,
        Side = side,
        Quantity = qty,
        Price = price,
        // Deterministic increasing timestamps so ordering is well-defined.
        Timestamp = new DateTimeOffset(2026, 1, 1, 9, minute, 0, TimeSpan.Zero)
    };

    [Fact] // (1) Long build-up: weighted-average cost.
    public void LongBuildUp_WeightedAverageCost()
    {
        var trades = new[]
        {
            T("AAPL", Side.Buy, 100, 10, 0),
            T("AAPL", Side.Buy, 100, 12, 1)
        };

        var pos = Assert.Single(PositionCalculator.Derive(trades));
        Assert.Equal("AAPL", pos.Symbol);
        Assert.Equal(200m, pos.NetQuantity);
        Assert.Equal(11m, pos.AverageCost);
    }

    [Fact] // (2) Selling part of a long reduces qty but leaves avg cost unchanged.
    public void PartialSell_LeavesAverageCostUnchanged()
    {
        var trades = new[]
        {
            T("AAPL", Side.Buy, 100, 10, 0),
            T("AAPL", Side.Buy, 100, 12, 1),
            T("AAPL", Side.Sell, 50, 15, 2)
        };

        var pos = Assert.Single(PositionCalculator.Derive(trades));
        Assert.Equal(150m, pos.NetQuantity);
        Assert.Equal(11m, pos.AverageCost); // unchanged despite selling at 15
    }

    [Fact] // (3) Headline case: mixed buys/sells flipping long -> short resets avg cost.
    public void FlipThroughZero_ResetsAverageCost()
    {
        var trades = new[]
        {
            T("AAPL", Side.Buy, 100, 10, 0),
            T("AAPL", Side.Buy, 100, 12, 1),
            T("AAPL", Side.Sell, 50, 15, 2),
            T("AAPL", Side.Sell, 200, 20, 3) // sells 200 against net 150 -> short 50 @ 20
        };

        var pos = Assert.Single(PositionCalculator.Derive(trades));
        Assert.Equal(-50m, pos.NetQuantity);
        Assert.Equal(20m, pos.AverageCost);
    }

    [Fact] // (4) A position closed to exactly zero is omitted from the result.
    public void ClosedToZero_SymbolOmitted()
    {
        var trades = new[]
        {
            T("MSFT", Side.Buy, 100, 50, 0),
            T("MSFT", Side.Sell, 100, 55, 1)
        };

        Assert.Empty(PositionCalculator.Derive(trades));
    }

    [Fact] // (5) Short build-up then partial cover — short-side symmetry of rules 1-3.
    public void ShortBuildUp_ThenPartialCover()
    {
        var trades = new[]
        {
            T("TSLA", Side.Sell, 100, 200, 0),
            T("TSLA", Side.Sell, 100, 220, 1), // short 200 @ avg 210
            T("TSLA", Side.Buy, 50, 190, 2)    // cover 50 -> short 150, avg unchanged
        };

        var pos = Assert.Single(PositionCalculator.Derive(trades));
        Assert.Equal(-150m, pos.NetQuantity);
        Assert.Equal(210m, pos.AverageCost);
    }

    [Fact] // (6) Symbols are computed independently.
    public void MultipleSymbols_ComputedIndependently()
    {
        var trades = new[]
        {
            T("AAPL", Side.Buy, 100, 10, 0),
            T("MSFT", Side.Buy, 50, 300, 1),
            T("AAPL", Side.Buy, 100, 20, 2)
        };

        var positions = PositionCalculator.Derive(trades);
        Assert.Equal(2, positions.Count);

        var aapl = positions.Single(p => p.Symbol == "AAPL");
        Assert.Equal(200m, aapl.NetQuantity);
        Assert.Equal(15m, aapl.AverageCost);

        var msft = positions.Single(p => p.Symbol == "MSFT");
        Assert.Equal(50m, msft.NetQuantity);
        Assert.Equal(300m, msft.AverageCost);
    }

    [Fact] // (7) Derivation processes in timestamp order regardless of input order.
    public void OrderingIsByTimestamp_NotInputOrder()
    {
        // Same trades as the flip case, but shuffled in the input array.
        var trades = new[]
        {
            T("AAPL", Side.Sell, 200, 20, 3),
            T("AAPL", Side.Buy, 100, 10, 0),
            T("AAPL", Side.Sell, 50, 15, 2),
            T("AAPL", Side.Buy, 100, 12, 1)
        };

        var pos = Assert.Single(PositionCalculator.Derive(trades));
        Assert.Equal(-50m, pos.NetQuantity);
        Assert.Equal(20m, pos.AverageCost);
    }

    [Fact] // Naked short: selling with no prior position opens a short.
    public void NakedShort_OpensNegativePosition()
    {
        var trades = new[] { T("NVDA", Side.Sell, 30, 100, 0) };

        var pos = Assert.Single(PositionCalculator.Derive(trades));
        Assert.Equal(-30m, pos.NetQuantity);
        Assert.Equal(100m, pos.AverageCost);
    }
}
