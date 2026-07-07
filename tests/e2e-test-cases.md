# End-to-End Test Cases (Playwright)

**Layer:** End-to-End · **Tooling:** Playwright (`frontend/e2e`) driving the **real Vue app against the real
.NET API** (SQLite pointed at a temp/test DB) · **Run:** `npm run test:e2e`

These `T-##` cases assert user-visible behavior through the browser: the trade-entry form, the live blotter
table (columns timestamp / symbol / side / quantity / price / notional, newest-first, sortable, color-coded
Buy/Sell), and the reactive positions panel. Every assertion is a DOM/UI observation.

**Shared preconditions (all cases):** the API and Vue dev/preview server are running; the app is loaded at
its base URL; the test DB starts **empty** (blotter and positions panels empty) unless steps say otherwise.
A key cross-cutting assertion: updates happen with **no full page reload** (verify via SPA navigation — e.g.
a sentinel set on `window` before submit survives, or no `load` event fires).

See the table legend in [`README.md §3`](./README.md#3-legend-for-the-test-case-tables).

---

| ID | Title | Preconditions | Input / Steps (user actions) | Expected Result (UI assertions) | Requirement ref |
|---|---|---|---|---|---|
| **T-01** | Valid trade → newest-first row + positions update, no reload | Empty blotter; set a `window` sentinel to detect reload | 1. In the form, type `symbol = AAPL`, select `side = Buy`, `quantity = 100`, `price = 10`. 2. Click **Submit**. 3. Submit a second trade `AAPL / Buy / 50 / 12`. | After step 2: a new blotter row appears **at the top** showing `AAPL`, `Buy`, `100`, `10`, `notional = 1000`, with a timestamp. Form clears. Positions panel shows `AAPL net +100, avg 10`. After step 3: the **new** row (`50 @12`) is now the top row (newest-first ordering holds); positions update to `AAPL net +150, avg 10.6667` (`(10·100+12·50)/150`). The reload sentinel is intact → **no page reload** occurred. | BR §5 · ARCH E2E #1 |
| **T-02** | Invalid trade → inline error, no row, no server write | Empty blotter; blotter row count = 0 | 1. Leave `symbol` empty (or set `quantity = 0`), fill the rest with valid values. 2. Click **Submit**. 3. Reload the page manually and re-inspect the blotter. | An **inline validation error** appears on the offending field; **no new blotter row** is added (count stays 0); positions panel unchanged. After the manual reload (step 3), the blotter is still empty → the invalid trade was **never written** to the server. | BR §5 · ARCH E2E #2 |
| **T-03** | Buy then Sell same symbol → correct net qty & avg cost | Empty blotter | 1. Submit `AAPL / Buy / 100 / 10`. 2. Submit `AAPL / Sell / 40 / 15`. 3. Read the positions panel. | Blotter shows 2 rows, the `Sell 40 @15` (`notional = 600`) on top, the `Buy 100 @10` (`notional = 1000`) below. Positions panel shows a single `AAPL` entry: `netQuantity = +60`, `averageCost = 10` (**avg unchanged** by the reducing sell — Rule 3). Buy row and Sell row are color-coded differently. | BR §3,§5 · ARCH E2E #3 |
| **T-04** | Sortable column header reorders the blotter | Empty blotter | 1. Submit three trades with distinct notionals, e.g. `AAPL Buy 100 @10` (1000), `MSFT Buy 10 @20` (200), `TSLA Buy 5 @300` (1500). 2. Click the **Notional** (or **Timestamp**) column header. 3. Click the same header again. | Default order is newest-first (TSLA, MSFT, AAPL by insertion time). After the first header click, rows reorder deterministically by that column (e.g. ascending notional: MSFT 200, AAPL 1000, TSLA 1500). A second click toggles to the reverse order (descending). Sort indicator reflects the active column/direction. | BR §5 · ARCH E2E #4 |
| **T-05** | Sequence nets to zero → symbol leaves positions, stays in blotter | Empty blotter | 1. Submit `NVDA / Buy / 50 / 20`. 2. Confirm positions shows `NVDA net +50, avg 20`. 3. Submit `NVDA / Sell / 50 / 22`. 4. Read positions and blotter. | After step 3 the positions panel has **no `NVDA` entry** (net = 0, omitted) — panel may be empty. The **blotter still shows both `NVDA` rows** (Sell `@22` newest on top, Buy `@20` below), each with its `notional` (`1100`, `1000`). Positions are derived, not stored. | BR §2,§3,§5 · ARCH E2E #5 |
| **T-06** | Buy/Sell color-coding is scannable | Empty blotter | 1. Submit `AAPL / Buy / 100 / 10`. 2. Submit `AAPL / Sell / 40 / 15`. 3. Inspect the two rows' side cells. | The **Buy** side is rendered with the buy styling (e.g. green pill) and the **Sell** side with the sell styling (e.g. red pill) — the two are visually distinct (distinct class / color), so the blotter is scannable at a glance. Numerics are right-aligned/monospaced. | BR §5 · ARCH §UI judgment |

---

### Availability & Error-Handling E2E cases

These `T-07…T-13` cases mirror, at the **browser/UI level**, the intent of the API-layer
`ServerAvailabilityTests` (`backend/TradeBlotter.Tests/E2E/ServerAvailabilityTests.cs`): the SPA must
**never surface a "server unavailable / not available / service unavailable" message under normal
operation**, and when the API genuinely fails it must **degrade gracefully** — a single
`[data-testid="global-error"]` banner, no crash, no blank page, no infinite spinner, and the UI stays
responsive. They exercise the store's `fetchAll` / `submitTrade` error paths (`src/stores/blotter.ts`) and
the `ApiError` handling in `src/api/client.ts`.

**How "server unavailable" is simulated in Playwright** (reproducible, no need to actually stop the .NET
process). Intercept the API routes with `page.route(...)` **before** `page.goto('/')` so the interception is
in effect during the SPA's `onMounted` → `fetchAll()`:

- **Connection refused / server down** — abort the request:
  `await page.route('**/trades', r => r.abort())` and `await page.route('**/positions', r => r.abort())`
  (the browser `fetch` rejects, exactly as if the API host were unreachable).
- **Service unavailable (503) / server error (5xx)** — fulfill with an error status:
  `await page.route('**/trades', r => r.fulfill({ status: 503, contentType: 'application/json', body: '{}' }))`.
- **Malformed / unexpected body** — fulfill with a non-JSON or unexpected payload:
  `r.fulfill({ status: 200, contentType: 'application/json', body: 'not json' })` (forces a parse/shape
  failure the SPA must catch).
- **Transient outage then recovery** — register an aborting/503 handler, assert the error, then
  `await page.unroute('**/trades')` (and `**/positions`) so subsequent calls hit the real API and succeed.
- **Slow-but-not-hung** — the app must not hang: assert the loading state clears and content or the error
  banner resolves within the spec timeout (e.g. `await expect(...).toBeVisible({ timeout: 10_000 })`), never
  an indefinite spinner.

Because these cases assert on **error/recovery** rather than exact seed rows, most start from the app's real
seed data (blotter + positions render) unless a route override is installed first.

| ID | Title | Preconditions | Input / Steps (user actions) | Expected Result (UI assertions) | Requirement ref |
|---|---|---|---|---|---|
| **T-07** | Backend reachable on load → renders, no error banner, no "unavailable" text | API and Vue server running normally; test DB seeded (or a known trade booked first) so at least one trade + one position exist; **no** `page.route` override installed | 1. `page.goto('/')`. 2. Wait for the app shell (`getByText('Trade Blotter')` visible). 3. Inspect the blotter, positions panel, and the top bar. | The blotter renders at least one `[data-testid="trade-row"]` (each with a `[data-testid="trade-symbol"]`) and the positions panel renders at least one `[data-testid="position-row"]` — i.e. seed/booked data is visible. `[data-testid="global-error"]` has **count 0** (banner not shown). The page text contains **no** "server unavailable", "not available", or "service unavailable" string. The loading state has cleared (no persistent spinner). | Graceful error handling · mirrors `ServerAvailabilityTests.ReadEndpoint_IsReachableAndSucceeds` / `_DoesNotReturnServiceUnavailable` |
| **T-08** | Backend unreachable on load → graceful global error, no crash, no blank page | Before navigation, install aborting handlers: `page.route('**/trades', r => r.abort())` **and** `page.route('**/positions', r => r.abort())` | 1. Register both aborting routes. 2. `page.goto('/')`. 3. Wait for the app shell to render. 4. Inspect the top bar, blotter, form, and console. | `fetchAll` rejects → `[data-testid="global-error"]` becomes **visible** with a graceful message (the store sets `error` from the caught error, falling back to `"Failed to load blotter data."`); the banner text **must not** read "server unavailable / not available". The app shell still renders (brand/header, empty blotter, the trade-entry form) — **not** a blank page and **no** uncaught exception surfaced to the user. The trade-entry inputs (`symbol-input`, `quantity-input`, `price-input`, `side-buy`/`side-sell`, `submit-trade`) remain present and interactable → UI stays responsive. | Graceful error handling · mirrors `ServerAvailabilityTests` (UI-level degradation) |
| **T-09** | Submit while backend down → clear error, no row added, typed input preserved | App loaded with seed data reachable (baseline blotter row count `N` captured); **then** install `page.route('**/trades', r => r.abort())` so only the POST/refresh fails | 1. Record `N = getByTestId('trade-row').count()`. 2. Install the aborting route for `**/trades`. 3. In the form type `symbol = MSFT`, `side = Buy`, `quantity = 25`, `price = 30`. 4. Click `submit-trade`. | The submit fails (network abort → non-`ApiError`): a clear error is surfaced (`[data-testid="global-error"]` visible **or** the absence of a success flash) and **no** `[data-testid="submit-flash"]` appears (count 0). **No** new blotter row is added — `getByTestId('trade-row').count()` stays `N`. The user's typed input is **not lost**: `symbol-input` still holds `MSFT`, `quantity-input` `25`, `price-input` `30` (form is not cleared on failure), so they can retry. No "service unavailable" copy shown. | Graceful error handling · BR §5 (no phantom write) |
| **T-10** | API validation error is a clean client error, not a "server unavailable" message | App loaded and API reachable (no route override); blotter row count = `N` | 1. Leave `symbol-input` empty (or set `quantity-input = 0`); fill the remaining fields with valid values (`price-input = 10`, pick `side-buy`). 2. Click `submit-trade`. 3. Inspect the field errors, the global banner, and the page text. | The API answers **400** (RFC 7807), surfaced as an `ApiError` and re-thrown to the form: inline field validation shows (`[data-testid="symbol-error"]` and/or `[data-testid="quantity-error"]` visible). Because a 400 is a handled `ApiError`, the **global** banner `[data-testid="global-error"]` stays **hidden** (count 0) and **no** `submit-flash` appears. The page shows **no** 5xx / "service unavailable" / "not available" text — the failure reads as a clean client-side validation error. Row count stays `N`. | Clean client error · mirrors `ServerAvailabilityTests.ValidationFailure_IsClientError_NotServerUnavailable` |
| **T-11** | Malformed / unexpected API response handled gracefully | Before navigation, override a read route to return an unexpected status/body, e.g. `page.route('**/positions', r => r.fulfill({ status: 503, contentType: 'application/json', body: '{}' }))` (variant: `status: 200` with `body: 'not json'`) | 1. Register the malformed/503 route for `**/positions` (leave `**/trades` real, or override both). 2. `page.goto('/')`. 3. Wait for the shell; inspect the error state and console. | `fetchAll` rejects on the bad response (`!response.ok` → `ApiError`, or JSON parse failure) and the store enters the error state: `[data-testid="global-error"]` is **visible** with a graceful message. **No** uncaught exception reaches the console as an unhandled rejection, and there is **no infinite spinner** — the loading state clears within the spec timeout. The app shell (header + form) still renders and stays interactive. | Graceful error handling · mirrors `ServerAvailabilityTests.MalformedRequest_IsClientError_NotServerFailure` |
| **T-12** | Recovery after transient outage → next action succeeds, error clears | Start in the T-08 failed-load state: aborting routes installed, `page.goto('/')`, `[data-testid="global-error"]` visible | 1. Confirm the global-error banner is visible (outage state). 2. Remove the overrides: `page.unroute('**/trades')` and `page.unroute('**/positions')` (API now reachable). 3. Trigger a fresh action — either book a valid trade (`symbol = RCVR`, `Buy`, `10`, `5`, click `submit-trade`) **or** reload the page. | The recovery action hits the live API and succeeds: on a booked trade, `[data-testid="submit-flash"]` appears and a new `[data-testid="trade-row"]` with `trade-symbol = RCVR` shows at the top with a matching `[data-testid="position-row"][data-symbol="RCVR"]`; on reload, the seed blotter/positions render. In **either** path the error state is **cleared** — `[data-testid="global-error"]` returns to count 0 (the store resets `error = null` at the start of `fetchAll`/`submitTrade`). No stale "unavailable" banner lingers. | Graceful recovery · ARCH §Availability |
| **T-13** | Initial load resolves promptly — app never hangs waiting on the server | App and API running normally (no override); a fresh page context | 1. `page.goto('/')`. 2. Assert content/settled state within a bounded timeout, e.g. `await expect(page.getByTestId('trade-row').first()).toBeVisible({ timeout: 10_000 })` (or, in the abort variant, the `global-error` banner within the same bound). 3. Confirm no persistent loading indicator remains. | The initial load **resolves promptly**: either the blotter/positions render **or** (if the API is down) the `[data-testid="global-error"]` banner shows — both within the bounded timeout, never an indefinite wait. The loading flag clears (`store.loading` → false via `finally`), so there is **no infinite spinner** and the app does not hang. This is the UI analogue of the server responding within a timeout. | No-hang / responsiveness · mirrors `ServerAvailabilityTests.Server_RespondsWithinTimeout` |

---

## Notes

- **No-reload check (T-01):** the reactive update is driven off the Pinia store re-fetching trades and
  positions after a successful POST; assert the SPA did not perform a full document reload (sentinel intact,
  or Playwright observes no navigation).
- **Determinism for sort (T-04):** choose trade notionals that are all distinct so the post-sort order is
  unambiguous; assert the concrete row order, not just "changed".
- **DB isolation:** each spec should start from a clean test DB (or a known seed) so row counts and
  newest-first ordering are deterministic across runs.
- These six front-to-back scenarios correspond to ARCH §Testing Strategy (d) E2E #1–#5, plus `T-06` covering
  the graded UI-scannability quality (color-coded Buy/Sell).
- **Availability cases (`T-07`…`T-13`):** the UI-level counterpart of the backend
  `E2E/ServerAvailabilityTests`. They assert the SPA never shows a "server unavailable / not available /
  service unavailable" message under normal operation, degrades gracefully to a single
  `[data-testid="global-error"]` banner when the API fails, preserves typed input on a failed submit, keeps
  the UI responsive, distinguishes a clean 400 validation error from a 5xx outage, and recovers (error clears)
  once the API is reachable again. Outages are simulated with Playwright route interception
  (`page.route(... r => r.abort())` for unreachable, `r.fulfill({ status: 503 })` for unavailable) and cleared
  with `page.unroute(...)` — no need to stop the real .NET process.
