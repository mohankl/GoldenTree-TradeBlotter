namespace TradeBlotter.Api.Dtos;

/// <summary>
/// Incoming payload for POST /trades. Id and Timestamp are assigned server-side.
/// Side is accepted as a string so an unknown value yields a clean 400 from our own
/// validation rather than a JSON binding 500.
/// </summary>
public record TradeRequest(string? Symbol, string? Side, decimal? Quantity, decimal? Price);
