using System.Net;

namespace TradeBlotter.Tests.E2E;

/// <summary>
/// End-to-end availability checks: the API is reachable and healthy, and it never
/// answers a normal request with a "server unavailable" style error (5xx / 503).
/// Even error responses (e.g. validation failures) are well-formed client errors,
/// not signals that the service is down.
/// </summary>
public class ServerAvailabilityTests : ApiTestBase
{
    public ServerAvailabilityTests(BlotterApiFactory factory) : base(factory) { }

    public static IEnumerable<object[]> ReadEndpoints => new[]
    {
        new object[] { "/trades" },
        new object[] { "/positions" },
    };

    [Theory] // Each read endpoint is reachable and returns a 2xx success.
    [MemberData(nameof(ReadEndpoints))]
    public async Task ReadEndpoint_IsReachableAndSucceeds(string path)
    {
        var response = await Client.GetAsync(path);

        Assert.True(response.IsSuccessStatusCode,
            $"GET {path} should succeed but returned {(int)response.StatusCode} {response.StatusCode}.");
    }

    [Theory] // The server never reports itself as unavailable (503).
    [MemberData(nameof(ReadEndpoints))]
    public async Task ReadEndpoint_DoesNotReturnServiceUnavailable(string path)
    {
        var response = await Client.GetAsync(path);
        Assert.NotEqual(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Theory] // No read endpoint returns a server-side (5xx) error.
    [MemberData(nameof(ReadEndpoints))]
    public async Task ReadEndpoint_DoesNotReturnServerError(string path)
    {
        var response = await Client.GetAsync(path);
        Assert.True((int)response.StatusCode < 500,
            $"GET {path} unexpectedly returned a server error: {(int)response.StatusCode}.");
    }

    [Fact] // A successful GET actually returns JSON content, confirming the app (not just the socket) is serving.
    public async Task ReadEndpoint_ServesJsonContent()
    {
        var response = await Client.GetAsync("/trades");
        response.EnsureSuccessStatusCode();

        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact] // A validation failure is a clean 400 client error — NOT a "server unavailable" message.
    public async Task ValidationFailure_IsClientError_NotServerUnavailable()
    {
        var response = await PostTrade(new { symbol = "", side = "Buy", quantity = 1, price = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("unavailable", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("not available", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("service unavailable", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact] // Even a malformed body yields a 4xx client error, not a 5xx outage.
    public async Task MalformedRequest_IsClientError_NotServerFailure()
    {
        var response = await PostRawJson("{ not valid json ]");

        Assert.True((int)response.StatusCode is >= 400 and < 500,
            $"Malformed body should be a 4xx client error but was {(int)response.StatusCode}.");
    }

    [Fact] // The server responds promptly (a hung/unavailable server would time out).
    public async Task Server_RespondsWithinTimeout()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var response = await Client.GetAsync("/positions", cts.Token);
        Assert.True(response.IsSuccessStatusCode);
    }
}
