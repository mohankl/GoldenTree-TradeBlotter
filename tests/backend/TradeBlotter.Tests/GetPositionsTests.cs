using System.Net;

namespace TradeBlotter.Tests;

/// <summary>
/// Tests for <c>GET /positions</c> — derived positions (net qty, avg cost) per
/// symbol, computed from trade history, with net-zero symbols omitted.
/// </summary>
public class GetPositionsTests : ApiTestBase
{
    public GetPositionsTests(BlotterApiFactory factory) : base(factory) { }

    [Fact] // Endpoint responds 200 OK.
    public async Task Returns200()
    {
        var response = await Client.GetAsync("/positions");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact] // A single buy yields a long position at that price.
    public async Task SingleBuy_ProducesLongPosition()
    {
        await PostTrade(new { symbol = "POS1", side = "Buy", quantity = 100, price = 20 });

        var pos = (await GetPositions()).Single(p => p.Symbol == "POS1");
        Assert.Equal(100m, pos.NetQuantity);
        Assert.Equal(20m, pos.AverageCost);
    }

    [Fact] // Two buys at different prices produce a weighted-average cost.
    public async Task TwoBuys_ProduceWeightedAverageCost()
    {
        await PostTrade(new { symbol = "POS2", side = "Buy", quantity = 100, price = 10 });
        await PostTrade(new { symbol = "POS2", side = "Buy", quantity = 100, price = 12 });

        var pos = (await GetPositions()).Single(p => p.Symbol == "POS2");
        Assert.Equal(200m, pos.NetQuantity);
        Assert.Equal(11m, pos.AverageCost);
    }

    [Fact] // Selling part of a long reduces quantity but leaves avg cost unchanged.
    public async Task PartialSell_LeavesAverageCostUnchanged()
    {
        await PostTrade(new { symbol = "POS3", side = "Buy", quantity = 200, price = 11 });
        await PostTrade(new { symbol = "POS3", side = "Sell", quantity = 50, price = 15 });

        var pos = (await GetPositions()).Single(p => p.Symbol == "POS3");
        Assert.Equal(150m, pos.NetQuantity);
        Assert.Equal(11m, pos.AverageCost);
    }

    [Fact] // Net-zero symbols are omitted from the response.
    public async Task NetZeroSymbol_IsOmitted()
    {
        await PostTrade(new { symbol = "POS4", side = "Buy", quantity = 100, price = 50 });
        await PostTrade(new { symbol = "POS4", side = "Sell", quantity = 100, price = 55 });

        Assert.DoesNotContain(await GetPositions(), p => p.Symbol == "POS4");
    }

    [Fact] // Positions for different symbols are computed independently.
    public async Task MultipleSymbols_AreIndependent()
    {
        await PostTrade(new { symbol = "POS5A", side = "Buy", quantity = 10, price = 100 });
        await PostTrade(new { symbol = "POS5B", side = "Sell", quantity = 5, price = 200 });

        var positions = await GetPositions();
        var a = positions.Single(p => p.Symbol == "POS5A");
        var b = positions.Single(p => p.Symbol == "POS5B");

        Assert.Equal(10m, a.NetQuantity);
        Assert.Equal(-5m, b.NetQuantity); // naked short -> negative net
    }

    [Fact] // The canonical worked example end-to-end through the HTTP path.
    public async Task WorkedExample_FlipsLongToShort()
    {
        await PostTrade(new { symbol = "POS6", side = "Buy", quantity = 100, price = 10 });
        await PostTrade(new { symbol = "POS6", side = "Buy", quantity = 100, price = 12 });
        await PostTrade(new { symbol = "POS6", side = "Sell", quantity = 50, price = 15 });
        await PostTrade(new { symbol = "POS6", side = "Sell", quantity = 200, price = 20 });

        var pos = (await GetPositions()).Single(p => p.Symbol == "POS6");
        Assert.Equal(-50m, pos.NetQuantity); // flipped short
        Assert.Equal(20m, pos.AverageCost);  // reset to flip price
    }
}
