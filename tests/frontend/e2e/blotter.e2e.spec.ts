import { test, expect, type Page } from '@playwright/test'

/**
 * End-to-end tests (browser ↔ Vue app ↔ .NET API). Each test uses a unique symbol
 * so it is independent of seed data and of other tests. See tests/e2e-test-cases.md
 * for the human-readable case descriptions (T-01…T-05).
 */

// Unique-ish symbol per test run without relying on Math.random (varied by suffix).
// Tickers are letters-only (enforced by the trade form), so the time-based suffix is
// encoded as letters: each digit 0-9 maps to A-J, preserving uniqueness.
function sym(base: string): string {
  const suffix = Date.now()
    .toString()
    .slice(-5)
    .replace(/[0-9]/g, (d) => 'ABCDEFGHIJ'[Number(d)])
  return `${base}${suffix}`
}

async function bookTrade(
  page: Page,
  { symbol, side, quantity, price }: { symbol: string; side: 'Buy' | 'Sell'; quantity: string; price: string },
) {
  await page.getByTestId('symbol-input').fill(symbol)
  await page.getByTestId(side === 'Buy' ? 'side-buy' : 'side-sell').click()
  await page.getByTestId('quantity-input').fill(quantity)
  await page.getByTestId('price-input').fill(price)
  // Submit and wait for the FULL cycle to finish (POST, then the store's positions
  // re-fetch) so a subsequent action doesn't race an in-flight submit — important
  // over higher-latency links (e.g. the live deployment).
  await Promise.all([
    page.waitForResponse((r) => r.url().includes('/positions') && r.request().method() === 'GET'),
    page.getByTestId('submit-trade').click(),
  ])
}

test.beforeEach(async ({ page }) => {
  await page.goto('/')
  // Order-ticket symbol input is always present — a stable readiness signal.
  await expect(page.getByTestId('symbol-input')).toBeVisible()
  // Let the initial trades/positions fetch settle so row counts are stable
  // (matters against the live deployment's larger, higher-latency dataset).
  await page.waitForLoadState('networkidle')
})

test('T-01: booking a valid trade adds a newest-first row and updates positions without reload', async ({ page }) => {
  const symbol = sym('AAA')
  await bookTrade(page, { symbol, side: 'Buy', quantity: '100', price: '10' })

  // A confirmation flash appears (submission succeeded without navigation).
  await expect(page.getByTestId('submit-flash')).toBeVisible()

  // The new trade is the first row in the blotter (newest-first).
  const firstRow = page.getByTestId('trade-row').first()
  await expect(firstRow.getByTestId('trade-symbol')).toHaveText(symbol)

  // The positions panel now shows the symbol.
  await expect(page.locator(`[data-testid="position-row"][data-symbol="${symbol}"]`)).toBeVisible()
})

test('T-02: invalid trade shows inline validation error and does not add a row', async ({ page }) => {
  const rowsBefore = await page.getByTestId('trade-row').count()

  // Empty symbol + zero quantity.
  await page.getByTestId('quantity-input').fill('0')
  await page.getByTestId('price-input').fill('10')
  await page.getByTestId('submit-trade').click()

  await expect(page.getByTestId('symbol-error')).toBeVisible()
  await expect(page.getByTestId('quantity-error')).toBeVisible()

  // No new row was added and no success flash shown.
  await expect(page.getByTestId('submit-flash')).toHaveCount(0)
  expect(await page.getByTestId('trade-row').count()).toBe(rowsBefore)
})

test('T-03: buy then sell the same symbol yields the correct net position and average cost', async ({ page }) => {
  const symbol = sym('BBB')
  await bookTrade(page, { symbol, side: 'Buy', quantity: '100', price: '10' })
  await bookTrade(page, { symbol, side: 'Buy', quantity: '100', price: '12' })

  const posRow = page.locator(`[data-testid="position-row"][data-symbol="${symbol}"]`)
  await expect(posRow).toBeVisible()
  await expect(posRow.getByTestId('position-qty')).toHaveText(/200/)
  await expect(posRow.getByTestId('position-avg')).toHaveText(/11/) // weighted avg of 10 & 12
})

test('T-04: clicking a sortable column header reorders the blotter', async ({ page }) => {
  // Book two trades with very different notionals under distinct symbols.
  const low = sym('LOW')
  const high = sym('HGH')
  await bookTrade(page, { symbol: low, side: 'Buy', quantity: '1', price: '1' }) // notional 1
  await bookTrade(page, { symbol: high, side: 'Buy', quantity: '1000', price: '1000' }) // notional 1,000,000

  // Sort by notional; toggling should bring the largest notional to the top.
  await page.getByTestId('sort-notional').click()
  const firstSymbol = page.getByTestId('trade-row').first().getByTestId('trade-symbol')

  // One of the two sort directions puts `high` first; click again if needed.
  if ((await firstSymbol.textContent())?.trim() !== high) {
    await page.getByTestId('sort-notional').click()
  }
  await expect(firstSymbol).toHaveText(high)
})

test('T-05: a sequence that nets to zero removes the symbol from positions but keeps it in the blotter', async ({ page }) => {
  const symbol = sym('ZER')
  await bookTrade(page, { symbol, side: 'Buy', quantity: '50', price: '20' })
  await expect(page.locator(`[data-testid="position-row"][data-symbol="${symbol}"]`)).toBeVisible()

  // Sell the entire position back to flat.
  await bookTrade(page, { symbol, side: 'Sell', quantity: '50', price: '25' })

  // Symbol is gone from positions...
  await expect(page.locator(`[data-testid="position-row"][data-symbol="${symbol}"]`)).toHaveCount(0)
  // ...but its trades remain in the blotter.
  await expect(page.getByTestId('trade-symbol').filter({ hasText: symbol }).first()).toBeVisible()
})
