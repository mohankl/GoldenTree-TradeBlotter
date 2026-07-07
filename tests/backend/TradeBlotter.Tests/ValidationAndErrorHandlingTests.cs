using System.Net;
using System.Net.Http.Json;

namespace TradeBlotter.Tests;

/// <summary>
/// Focused, exhaustive coverage of input validation and error handling for
/// <c>POST /trades</c>: every rule (non-empty symbol; side ∈ {Buy,Sell};
/// quantity &gt; 0; price &gt; 0), malformed bodies, problem-details shape, and the
/// guarantee that rejected trades are never persisted.
/// </summary>
public class ValidationAndErrorHandlingTests : ApiTestBase
{
    public ValidationAndErrorHandlingTests(BlotterApiFactory factory) : base(factory) { }

    // ---- Symbol ---------------------------------------------------------

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Symbol_EmptyOrWhitespace_Returns400(string symbol)
    {
        var response = await PostTrade(new { symbol, side = "Buy", quantity = 10, price = 5 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Symbol_Missing_Returns400()
    {
        var response = await PostTrade(new { side = "Buy", quantity = 10, price = 5 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---- Side -----------------------------------------------------------

    [Theory]
    [InlineData("Hodl")]
    [InlineData("B")]
    [InlineData("123")]
    [InlineData("")]
    public async Task Side_Invalid_Returns400(string side)
    {
        var response = await PostTrade(new { symbol = "AAPL", side, quantity = 10, price = 5 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Side_Missing_Returns400()
    {
        var response = await PostTrade(new { symbol = "AAPL", quantity = 10, price = 5 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---- Quantity -------------------------------------------------------

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-0.5)]
    public async Task Quantity_NonPositive_Returns400(decimal quantity)
    {
        var response = await PostTrade(new { symbol = "AAPL", side = "Buy", quantity, price = 5 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Quantity_Missing_Returns400()
    {
        var response = await PostTrade(new { symbol = "AAPL", side = "Buy", price = 5 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---- Price ----------------------------------------------------------

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(-0.01)]
    public async Task Price_NonPositive_Returns400(decimal price)
    {
        var response = await PostTrade(new { symbol = "AAPL", side = "Buy", quantity = 10, price });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Price_Missing_Returns400()
    {
        var response = await PostTrade(new { symbol = "AAPL", side = "Buy", quantity = 10 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---- Multiple / malformed ------------------------------------------

    [Fact]
    public async Task AllFieldsInvalid_Returns400()
    {
        var response = await PostTrade(new { symbol = "", side = "Nope", quantity = -1, price = 0 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task MalformedJsonBody_Returns400()
    {
        var response = await PostRawJson("{ this is not json ]");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task WrongTypeForQuantity_Returns400()
    {
        // quantity as a non-numeric string -> body binding fails -> 400.
        var response = await PostRawJson("""{ "symbol": "AAPL", "side": "Buy", "quantity": "lots", "price": 5 }""");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---- Problem-details shape & side effects --------------------------

    [Fact]
    public async Task ValidationError_ReturnsProblemDetailsWithFieldErrors()
    {
        var response = await PostTrade(new { symbol = "", side = "Buy", quantity = 0, price = 0 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemResponse>(Json);
        Assert.NotNull(problem);
        Assert.NotNull(problem!.Errors);
        // Field-level messages are present for the invalid fields.
        Assert.True(problem.Errors!.ContainsKey("symbol"));
        Assert.True(problem.Errors.ContainsKey("quantity"));
        Assert.True(problem.Errors.ContainsKey("price"));
    }

    [Fact]
    public async Task RejectedTrade_IsNotPersisted()
    {
        var before = (await GetTrades()).Count;

        await PostTrade(new { symbol = "", side = "Buy", quantity = 10, price = 5 });        // invalid
        await PostTrade(new { symbol = "REJECT", side = "Buy", quantity = -5, price = 5 });   // invalid

        var after = (await GetTrades()).Count;
        Assert.Equal(before, after);
        Assert.DoesNotContain(await GetTrades(), t => t.Symbol == "REJECT");
    }

    private record ValidationProblemResponse(string? Title, int? Status, Dictionary<string, string[]>? Errors);
}
