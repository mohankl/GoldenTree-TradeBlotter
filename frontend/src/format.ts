// Shared number/currency formatters. Centralized so every panel renders money and
// quantities identically (and Intl formatter instances are created once, not per component).

/** USD currency, e.g. 230.5 -> "$230.50". */
export const currency = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' })

/** Share quantity, up to 4 decimals and grouped, e.g. 1234.5 -> "1,234.5". */
export const qtyFmt = new Intl.NumberFormat('en-US', { maximumFractionDigits: 4 })

/** Compact USD for headline figures: "$1.25M", "$137.5k", or full currency below 1,000. */
export function compactUsd(n: number): string {
  if (n >= 1_000_000) return `$${(n / 1_000_000).toFixed(2)}M`
  if (n >= 1_000) return `$${(n / 1_000).toFixed(1)}k`
  return currency.format(n)
}
