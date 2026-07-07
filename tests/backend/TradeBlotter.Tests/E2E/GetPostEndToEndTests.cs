using System.Net;
using System.Net.Http.Json;

namespace TradeBlotter.Tests.E2E;

/// <summary>
/// End-to-end round trips through the live HTTP pipeline confirming the GET and
/// POST functions work together: a POSTed trade is persisted and then observable
/// via GET /trades, and the derived GET /positions reflects it.
/// </summary>
public class GetPostEndToEndTests : ApiTestBase
{
    public GetPostEndToEndTests(BlotterApiFactory factory) : base(factory) { }

    [Fact] // GET /trades works: reachable, 200, returns a JSON array.
    public async Task Get_Trades_Works()
    {
        var response = await Client.GetAsync("/trades");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var trades = await response.Content.ReadFromJsonAsync<List<TradeDto>>(Json);
        Assert.NotNull(trades); // a JSON array (possibly empty), never null
    }

    [Fact] // GET /positions works: reachable, 200, returns a JSON array.
    public async Task Get_Positions_Works()
    {
        var response = await Client.GetAsync("/positions");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var positions = await response.Content.ReadFromJsonAsync<List<PositionDto>>(Json);
        Assert.NotNull(positions);
    }

    [Fact] // POST /trades works: a valid trade is accepted with 201 Created.
    public async Task Post_Trade_Works()
    {
        var response = await PostTrade(new { symbol = "E2E", side = "Buy", quantity = 10, price = 100 });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact] // Full round trip: POST a trade, then GET it back from /trades.
    public async Task Post_ThenGet_Trades_RoundTrip()
    {
        var create = await PostTrade(new { symbol = "ROUND", side = "Buy", quantity = 25, price = 40 });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<TradeDto>(Json);

        var trades = await GetTrades();

        // The trade we created is present, with matching id and values.
        var found = trades.SingleOrDefault(t => t.Id == created!.Id);
        Assert.NotNull(found);
        Assert.Equal("ROUND", found!.Symbol);
        Assert.Equal(25m, found.Quantity);
        Assert.Equal(40m, found.Price);
        Assert.Equal(1000m, found.Notional); // 25 * 40
    }

    [Fact] // Full round trip: POST trades, then GET the derived position.
    public async Task Post_ThenGet_Positions_Reflects_Trades()
    {
        await PostTrade(new { symbol = "PANDG", side = "Buy", quantity = 100, price = 10 });
        await PostTrade(new { symbol = "PANDG", side = "Buy", quantity = 100, price = 20 });

        var pos = (await GetPositions()).Single(p => p.Symbol == "PANDG");
        Assert.Equal(200m, pos.NetQuantity);
        Assert.Equal(15m, pos.AverageCost); // weighted average of 10 and 20
    }

    [Fact] // The newly created trade increases the GET /trades count by exactly one.
    public async Task Post_IncrementsTradeCountByOne()
    {
        var before = (await GetTrades()).Count;
        await PostTrade(new { symbol = "COUNT", side = "Buy", quantity = 1, price = 1 });
        var after = (await GetTrades()).Count;

        Assert.Equal(before + 1, after);
    }
}
