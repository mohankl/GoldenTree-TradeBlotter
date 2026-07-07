using System.Net;
using System.Net.Http.Json;

namespace TradeBlotter.Tests;

/// <summary>
/// Tests for <c>POST /trades</c> — submitting a new trade (happy paths).
/// Validation and error handling are covered separately in
/// <see cref="ValidationAndErrorHandlingTests"/>.
/// </summary>
public class PostTradesTests : ApiTestBase
{
    public PostTradesTests(BlotterApiFactory factory) : base(factory) { }

    [Fact] // Valid buy returns 201 Created with the enriched, persisted trade.
    public async Task ValidBuy_Returns201_WithCreatedTrade()
    {
        var response = await PostTrade(new { symbol = "AAPL", side = "Buy", quantity = 10, price = 5 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var trade = await response.Content.ReadFromJsonAsync<TradeDto>(Json);
        Assert.NotNull(trade);
        Assert.NotEqual(Guid.Empty, trade!.Id);
        Assert.Equal("Buy", trade.Side);
        Assert.Equal(10m, trade.Quantity);
        Assert.Equal(5m, trade.Price);
    }

    [Fact] // 201 responses set a Location header pointing at the created resource.
    public async Task ValidTrade_SetsLocationHeader()
    {
        var response = await PostTrade(new { symbol = "MSFT", side = "Buy", quantity = 1, price = 400 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }

    [Fact] // Notional is computed server-side as quantity * price.
    public async Task Response_IncludesComputedNotional()
    {
        var response = await PostTrade(new { symbol = "NVDA", side = "Buy", quantity = 3, price = 140 });
        var trade = await response.Content.ReadFromJsonAsync<TradeDto>(Json);

        Assert.Equal(420m, trade!.Notional); // 3 * 140
    }

    [Fact] // Symbols are normalized to upper-case.
    public async Task Symbol_IsNormalizedToUpperCase()
    {
        var response = await PostTrade(new { symbol = "  goog  ", side = "Buy", quantity = 2, price = 185 });
        var trade = await response.Content.ReadFromJsonAsync<TradeDto>(Json);

        Assert.Equal("GOOG", trade!.Symbol);
    }

    [Fact] // Side is accepted case-insensitively (e.g. "sell").
    public async Task Side_IsParsedCaseInsensitively()
    {
        var response = await PostTrade(new { symbol = "BAC", side = "sell", quantity = 5, price = 45 });
        var trade = await response.Content.ReadFromJsonAsync<TradeDto>(Json);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("Sell", trade!.Side);
    }

    [Fact] // The server assigns a UTC timestamp at (or near) submission time.
    public async Task Server_AssignsUtcTimestamp()
    {
        var before = DateTimeOffset.UtcNow.AddSeconds(-5);
        var response = await PostTrade(new { symbol = "JPM", side = "Buy", quantity = 1, price = 250 });
        var after = DateTimeOffset.UtcNow.AddSeconds(5);

        var trade = await response.Content.ReadFromJsonAsync<TradeDto>(Json);
        Assert.InRange(trade!.Timestamp, before, after);
    }

    [Fact] // A submitted trade is actually persisted and shows up in GET /trades.
    public async Task SubmittedTrade_IsPersisted()
    {
        await PostTrade(new { symbol = "PERSIST", side = "Buy", quantity = 7, price = 12 });

        var trades = await GetTrades();
        Assert.Contains(trades, t => t.Symbol == "PERSIST" && t.Quantity == 7m);
    }

    [Fact] // Fractional quantity and price are accepted and preserved exactly.
    public async Task FractionalQuantityAndPrice_ArePreserved()
    {
        var response = await PostTrade(new { symbol = "FRAC", side = "Buy", quantity = 0.5m, price = 10.125m });
        var trade = await response.Content.ReadFromJsonAsync<TradeDto>(Json);

        Assert.Equal(0.5m, trade!.Quantity);
        Assert.Equal(10.125m, trade.Price);
        Assert.Equal(5.0625m, trade.Notional);
    }
}
