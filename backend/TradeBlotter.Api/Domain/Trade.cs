namespace TradeBlotter.Api.Domain;

/// <summary>
/// An immutable record of a single trade execution. This is the only entity that is
/// persisted; positions are always derived from the full set of trades.
/// </summary>
public class Trade
{
    public Guid Id { get; set; }

    /// <summary>Ticker symbol, normalized to upper-case (e.g. AAPL).</summary>
    public string Symbol { get; set; } = string.Empty;

    public Side Side { get; set; }

    /// <summary>Number of shares. Always positive; direction is carried by <see cref="Side"/>.</summary>
    public decimal Quantity { get; set; }

    /// <summary>Execution price per share. Always positive.</summary>
    public decimal Price { get; set; }

    /// <summary>Server-assigned execution time (UTC).</summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>Signed quantity: Buy = +Quantity, Sell = -Quantity.</summary>
    public decimal SignedQuantity => Side == Side.Buy ? Quantity : -Quantity;
}
