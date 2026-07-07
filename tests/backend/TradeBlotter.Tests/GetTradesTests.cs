using System.Net;

namespace TradeBlotter.Tests;

/// <summary>
/// Tests for <c>GET /trades</c> — returning all trades, newest first, with notional.
/// </summary>
public class GetTradesTests : ApiTestBase
{
    public GetTradesTests(BlotterApiFactory factory) : base(factory) { }

    [Fact] // Endpoint responds 200 OK.
    public async Task Returns200()
    {
        var response = await Client.GetAsync("/trades");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact] // Every submitted trade appears in the list.
    public async Task ReturnsAllSubmittedTrades()
    {
        await PostTrade(new { symbol = "LIST1", side = "Buy", quantity = 1, price = 10 });
        await PostTrade(new { symbol = "LIST2", side = "Sell", quantity = 2, price = 20 });

        var trades = await GetTrades();
        Assert.Contains(trades, t => t.Symbol == "LIST1");
        Assert.Contains(trades, t => t.Symbol == "LIST2");
    }

    [Fact] // Trades are ordered newest-first by timestamp.
    public async Task Returns_NewestFirst()
    {
        await PostTrade(new { symbol = "ORDERA", side = "Buy", quantity = 1, price = 1 });
        await Task.Delay(10);
        await PostTrade(new { symbol = "ORDERB", side = "Buy", quantity = 1, price = 1 });
        await Task.Delay(10);
        await PostTrade(new { symbol = "ORDERC", side = "Buy", quantity = 1, price = 1 });

        var trades = await GetTrades();
        var a = trades.FindIndex(t => t.Symbol == "ORDERA");
        var b = trades.FindIndex(t => t.Symbol == "ORDERB");
        var c = trades.FindIndex(t => t.Symbol == "ORDERC");

        // Newest (C) first, oldest (A) last.
        Assert.True(c < b && b < a, "Trades should be ordered newest-first");
    }

    [Fact] // The overall list is sorted descending by timestamp.
    public async Task List_IsSortedDescendingByTimestamp()
    {
        await PostTrade(new { symbol = "SORTED", side = "Buy", quantity = 1, price = 1 });
        var trades = await GetTrades();

        for (var i = 1; i < trades.Count; i++)
        {
            Assert.True(trades[i - 1].Timestamp >= trades[i].Timestamp,
                "Each trade's timestamp should be >= the next one's");
        }
    }

    [Fact] // Each row includes the computed notional value.
    public async Task EachTrade_IncludesNotional()
    {
        await PostTrade(new { symbol = "NOTL", side = "Buy", quantity = 4, price = 25 });

        var trade = (await GetTrades()).First(t => t.Symbol == "NOTL");
        Assert.Equal(100m, trade.Notional); // 4 * 25
    }
}
