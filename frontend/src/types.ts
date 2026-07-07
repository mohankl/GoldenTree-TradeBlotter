// Shared domain types mirroring the backend contract.

export type Side = 'Buy' | 'Sell'

export interface Trade {
  id: string
  symbol: string
  side: Side
  quantity: number
  price: number
  notional: number
  timestamp: string // ISO-8601 (UTC) from the server
}

export interface Position {
  symbol: string
  netQuantity: number // signed: positive = long, negative = short
  averageCost: number
}

/** Payload for creating a trade (id/timestamp/notional are server-assigned). */
export interface TradeInput {
  symbol: string
  side: Side
  quantity: number
  price: number
}

/** Shape of an RFC 7807 validation-problem response from the API. */
export interface ValidationProblem {
  title?: string
  status?: number
  errors?: Record<string, string[]>
}
