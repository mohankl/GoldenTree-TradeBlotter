using TradeBlotter.Api.Domain;

namespace TradeBlotter.Api.Dtos;

/// <summary>Outgoing representation of a trade, including the derived notional value.</summary>
public record TradeResponse(
    Guid Id,
    string Symbol,
    Side Side,
    decimal Quantity,
    decimal Price,
    decimal Notional,
    DateTimeOffset Timestamp)
{
    public static TradeResponse From(Trade t) =>
        new(t.Id, t.Symbol, t.Side, t.Quantity, t.Price, t.Quantity * t.Price, t.Timestamp);
}
