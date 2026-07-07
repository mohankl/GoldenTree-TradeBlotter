import type { Trade, Position, TradeInput, ValidationProblem } from '../types'

// In dev, VITE_API_URL is unset -> relative paths that the Vite proxy forwards to
// the .NET API. In production (two-service deploy), VITE_API_URL is the backend's
// public URL, baked in at build time, so the SPA calls the API cross-origin (CORS).
const API_BASE = (import.meta.env.VITE_API_URL ?? '').replace(/\/$/, '')
const TRADES_URL = `${API_BASE}/trades`
const POSITIONS_URL = `${API_BASE}/positions`

/** Thrown when the API rejects a request; carries field-level validation errors. */
export class ApiError extends Error {
  constructor(
    message: string,
    public readonly status: number,
    public readonly fieldErrors: Record<string, string[]> = {},
  ) {
    super(message)
    this.name = 'ApiError'
  }
}

async function parseError(response: Response): Promise<ApiError> {
  let problem: ValidationProblem | undefined
  try {
    problem = await response.json()
  } catch {
    // Non-JSON error body; fall back to the status text.
  }
  const message = problem?.title ?? `Request failed (${response.status})`
  return new ApiError(message, response.status, problem?.errors ?? {})
}

export const api = {
  async getTrades(): Promise<Trade[]> {
    const response = await fetch(TRADES_URL)
    if (!response.ok) throw await parseError(response)
    return response.json()
  },

  async getPositions(): Promise<Position[]> {
    const response = await fetch(POSITIONS_URL)
    if (!response.ok) throw await parseError(response)
    return response.json()
  },

  async createTrade(input: TradeInput): Promise<Trade> {
    const response = await fetch(TRADES_URL, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(input),
    })
    if (!response.ok) throw await parseError(response)
    return response.json()
  },
}
