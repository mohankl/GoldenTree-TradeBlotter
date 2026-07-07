using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TradeBlotter.Tests;

/// <summary>
/// Shared plumbing for the HTTP-level API tests: a client from the isolated
/// in-memory factory, JSON options matching the API contract, response DTOs, and
/// small request helpers. Each endpoint has its own test class deriving from this.
/// </summary>
public abstract class ApiTestBase : IClassFixture<BlotterApiFactory>
{
    protected readonly HttpClient Client;

    protected static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    protected ApiTestBase(BlotterApiFactory factory) => Client = factory.CreateClient();

    protected record TradeDto(
        Guid Id, string Symbol, string Side, decimal Quantity, decimal Price, decimal Notional, DateTimeOffset Timestamp);

    protected record PositionDto(string Symbol, decimal NetQuantity, decimal AverageCost);

    protected Task<HttpResponseMessage> PostTrade(object body) =>
        Client.PostAsJsonAsync("/trades", body, Json);

    /// <summary>POST a raw JSON string (for malformed-body / bad-type tests).</summary>
    protected Task<HttpResponseMessage> PostRawJson(string json)
    {
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        return Client.PostAsync("/trades", content);
    }

    protected async Task<List<TradeDto>> GetTrades() =>
        (await Client.GetFromJsonAsync<List<TradeDto>>("/trades", Json))!;

    protected async Task<List<PositionDto>> GetPositions() =>
        (await Client.GetFromJsonAsync<List<PositionDto>>("/positions", Json))!;
}
