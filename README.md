# GoldenTree · Trade Blotter

A full-stack **trade blotter** — the kind of tool a trader uses to book trades and watch positions build in real time. A user enters a trade, it appears instantly in a live blotter table, and the positions panel recomputes **net quantity and average cost per symbol**. Positions are always **derived** from the trade history, never stored.

**Tech tags:** `C#` · `.NET 10` · `Minimal API` · `EF Core` · `SQLite` · `Vue 3` · `Composition API` · `Pinia` · `Vite` · `TypeScript` · `xUnit` · `Vitest` · `Playwright`

---

## Table of Contents

1. [Objective](#1-objective)
2. [What the app does](#2-what-the-app-does)
3. [Tech stack](#3-tech-stack)
4. [Project layout](#4-project-layout)
5. [Prerequisites](#5-prerequisites)
6. [Running the Backend](#6-running-the-backend)
7. [Running the Frontend](#7-running-the-frontend)
8. [Testing](#8-testing)
   - [8A. Backend testing](#8a-backend-testing)
   - [8B. Frontend testing](#8b-frontend-testing)
9. [Test cases](#9-test-cases)
10. [API reference](#10-api-reference)
11. [Design decisions](#11-design-decisions--assumptions)
12. [Given more time](#12-given-more-time)

---

## Live Deployment

Both services are deployed on **Railway** (project *GoldenTree-TradeBlotter*), each a separate Dockerfile-built service:

| | URL |
|---|---|
| **App (Vue SPA)** | https://frontend-production-1237b.up.railway.app |
| **API (.NET 10)** | https://backend-production-bf13.up.railway.app — try [`/positions`](https://backend-production-bf13.up.railway.app/positions) · [`/trades`](https://backend-production-bf13.up.railway.app/trades) |

The SPA is built with the API's public URL baked in (`VITE_API_URL`) and calls it cross-origin (CORS enabled on the API). Verified live: all 5 Playwright E2E scenarios pass against the deployment.

> The **"Trading Desk" UI redesign** is deployed to the existing Railway frontend service at the same URL above (presentation-only change; API, data-testids, and contracts unchanged).

## 1. Objective

The objective is a small but polished slice of a real trading tool that demonstrates end-to-end full-stack engineering:

- **Clean domain modeling** — well-reasoned `Trade` and `Position` types, with correct **average-cost logic on mixed buys and sells** (including long↔short flips).
- **Sound API design** — clear REST contracts, sensible validation, and appropriate HTTP status codes.
- **Idiomatic Vue 3** — Composition API used naturally, reactivity handled cleanly, all shared state in a single Pinia store.
- **UI judgment** — a "Trading Desk" layout: a top **order-ticket command bar** for entry, a **KPI summary strip**, and a **side-striped blotter** (color-coded Buy/Sell, monospaced tabular numbers) beside **position cards** — scannable at a glance.
- **Tested correctness** — the position-calculation logic is covered by unit tests, backed by per-endpoint, validation, edge-case, and end-to-end tests.

Guiding principle: **quality over completeness** — a working, polished core over a broad but rough feature set.

## 2. What the app does

The UI follows a **"Trading Desk" layout**: a horizontal **order-ticket command bar** across the top (Symbol · Buy/Sell toggle · Quantity · Price · Book Trade), a **KPI summary strip** beneath it (Open Positions, Trades, Gross Exposure, Net Bias), then a two-column body — a dominant **blotter** on the left and a **positions rail** on the right.

### Trade entry
- Book a trade with **symbol, side (Buy/Sell), quantity, price** via the top **order-ticket command bar** (side is a segmented Buy/Sell toggle).
- **Client-side validation** mirrors the server: no empty fields; quantity and price must be positive; inline field errors.
- On submit, the blotter and positions update **immediately, without a page reload**.

### Live blotter
- Lists **all trades, newest first** — columns **Time, Symbol, Side, Quantity, Price, Notional** (`quantity × price`).
- **Sortable** by any column (sticky headers; click to toggle direction).
- Each row carries a green/red **side-stripe** (left border) and a color-coded Buy/Sell pill; monospaced tabular-nums with notional highlighted in gold for fast scanning.

### Positions panel
- A **rail of position cards** showing **net position and average cost per symbol**, derived from the full trade history.
- Each card has a **LONG/SHORT** chip, Net Qty, Avg Cost, Cost Basis, and a small **exposure bar**; net-zero symbols are omitted automatically.
- **Updates reactively** whenever a trade is booked.

### Position / average-cost logic (the core domain rule)
Trades are replayed oldest→newest per symbol, tracking a signed net quantity and average cost:

| Situation | Effect on average cost |
|---|---|
| Opening from flat | Avg = trade price |
| Increasing exposure (same direction) | Weighted-average the new fill in |
| Reducing exposure (not crossing zero) | **Unchanged** (the closed portion realizes P&L) |
| Flipping through zero (long→short or vice-versa) | **Resets** to the flipping trade's price |

**Worked example:** Buy 100 @10, Buy 100 @12 → **200 @11**; Sell 50 @15 → **150 @11**; Sell 200 @20 → **short 50 @20**.

### Seed data
On first run the backend seeds **28 realistic demo trades** across banks (**WFC, BAC, JPM**) and tech (**GOOGL, MSFT, AAPL, NVDA**) — including a long→short flip (BAC) and a fully-closed net-zero symbol (WFC).

## 3. Tech stack

| Layer | Technology |
|---|---|
| **Backend runtime** | C# / **.NET 10** (minimal APIs) |
| **Persistence** | **SQLite** via **Entity Framework Core** (trades only; positions derived on read) |
| **Backend tests** | **xUnit** + `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`) |
| **Frontend framework** | **Vue 3** (Composition API, `<script setup>`) |
| **State management** | **Pinia** (single source of truth) |
| **Build tooling** | **Vite** + **TypeScript** |
| **Frontend unit tests** | **Vitest** + **Vue Test Utils** |
| **End-to-end tests** | **Playwright** |
| **Styling** | Hand-written CSS (dark "trading terminal" theme); GoldenTree-inspired brand palette |

> **.NET version note:** the exercise brief suggested .NET 8; this machine has the **.NET 10 SDK**, so the project targets `net10.0`. No version-specific features are used — changing the `TargetFramework` would build it on .NET 8.

## 4. Project layout

```
GoldenTree-TradeBlotter/
├── backend/                        # .NET 10 solution
│   ├── TradeBlotter.slnx            #   solution (references API + test project)
│   ├── Dockerfile                   #   backend container image
│   └── TradeBlotter.Api/            #   minimal API: Domain, Dtos, Data, Endpoints
├── frontend/                       # Vue 3 + Vite + Pinia SPA
│   ├── Dockerfile                   #   frontend container image
│   ├── vite.config.ts               #   Vite + Vitest config
│   ├── playwright.config.ts         #   Playwright config (auto-starts both servers)
│   └── src/                         #   stores, api client, components, styles
├── tests/                          # ALL tests + the human-readable test-case catalog
│   ├── backend/TradeBlotter.Tests/  #   xUnit: unit, per-endpoint, validation, E2E
│   ├── frontend/unit/               #   Vitest form-validation tests
│   ├── frontend/e2e/                #   Playwright end-to-end specs
│   └── *.md                         #   test-case catalog + traceability matrix
├── brand-assets/                   # Palette, logos, favicon, icons, guidelines
├── business-requirements.md        # Living requirements doc
├── project-architecture.md         # Architecture & design doc
└── cloud-session.md                # AI-assisted development session log
```

## 5. Prerequisites

- **.NET SDK 10** — verify with `dotnet --version`
- **Node.js 20+** and **npm** — verify with `node --version` and `npm --version`

The app runs as **two processes in two terminals**: the **backend API** and the **frontend SPA**. Start the **backend first**, then the frontend.

---

## 6. Running the Backend

> **Terminal 1.** Keep it running while you use the app.

**Step 1 — Go to the API project:**
```bash
cd backend/TradeBlotter.Api
```

**Step 2 — Start the API:**
```bash
dotnet run
```

**Step 3 — Confirm it's up.** You should see `Now listening on: http://localhost:5000`.
- API base URL: **http://localhost:5000**
- On first launch it creates the SQLite file `blotter.db` and seeds the demo trades.

**Step 4 — (Optional) smoke-test the endpoints** (from any terminal):
```bash
curl http://localhost:5000/trades       # all trades, newest first (with notional)
curl http://localhost:5000/positions    # derived positions (net qty + avg cost)

curl -X POST http://localhost:5000/trades \
  -H 'Content-Type: application/json' \
  -d '{"symbol":"AAPL","side":"Buy","quantity":100,"price":230.50}'
```

**Stop the backend:** `Ctrl+C` in Terminal 1.

---

## 7. Running the Frontend

> **Terminal 2.** The backend (section 6) must already be running.

**Step 1 — Go to the frontend project:**
```bash
cd frontend
```

**Step 2 — Install dependencies** (first time only):
```bash
npm install
```

**Step 3 — Start the Vite dev server:**
```bash
npm run dev
```

**Step 4 — Open the app** at **http://localhost:5173**.
- The dev server **proxies** `/trades` and `/positions` to the backend on port 5000 — **no CORS setup needed**.
- You should see the blotter populated with seed data, the entry form, and the live positions panel. Book a trade and watch both update instantly.

**Stop the frontend:** `Ctrl+C` in Terminal 2.

### Quick reference

| Component | Directory | Command | URL |
|---|---|---|---|
| **Backend** (Terminal 1) | `backend/TradeBlotter.Api` | `dotnet run` | http://localhost:5000 |
| **Frontend** (Terminal 2) | `frontend` | `npm install` then `npm run dev` | http://localhost:5173 |

---

## 8. Testing

All automated tests live under [`tests/`](tests/), split into **`backend/`** and **`frontend/`**. Current status: **65 backend + 6 frontend-unit + 5 end-to-end = 76 passing**.

### 8A. Backend testing

> xUnit. Runs from the `backend/` solution (test project lives in `tests/backend/TradeBlotter.Tests/`).

```bash
cd backend
dotnet test
```

What it covers:
- **Position logic (unit)** — weighted average, sell-leaves-avg-unchanged, flip reset, net-zero omission, short-side symmetry, multi-symbol isolation, timestamp ordering.
- **Per endpoint** — `POST /trades`, `GET /trades` (newest-first + notional), `GET /positions` (derived, net-zero omitted).
- **Validation & error handling** — every rule (empty symbol, bad side, non-positive qty/price), malformed JSON, wrong types, RFC 7807 problem-details shape, and that rejected trades are never persisted.
- **End-to-end (`E2E/` folder)** — server availability (never returns a "service unavailable"/5xx for normal requests) and full GET/POST round trips.

### 8B. Frontend testing

> Two suites: Vitest (unit) and Playwright (end-to-end).

**Unit — Vitest (form validation), from `tests/frontend/unit/`:**
```bash
cd frontend
npm run test:unit
```
Covers: empty symbol blocks submit; non-positive quantity/price blocked; empty/non-numeric handling; valid input submits exactly once with the correct payload and resets; side toggle reflected in the payload.

**End-to-end — Playwright (browser ↔ Vue ↔ API), from `tests/frontend/e2e/`:**
```bash
cd frontend
npx playwright install    # first time only (downloads the browser)
npm run test:e2e
```
Playwright **automatically starts both the backend and frontend** (reusing them if already running), then drives real user flows: booking a valid trade updates the blotter (newest-first) and positions with no reload; invalid input shows inline errors and adds no row; buy-then-buy shows correct net + average cost; sorting reorders the blotter; a net-zero sequence removes the symbol from positions but keeps it in the blotter.

---

## 9. Test cases

A human-readable **test-case catalog** (independent of the code) lives in [`tests/`](tests/):

| File | Contents |
|---|---|
| [`tests/README.md`](tests/README.md) | Test strategy overview, layers, how to run each suite |
| [`tests/unit-test-cases.md`](tests/unit-test-cases.md) | Unit cases `U-01…` (position logic) and `U-F01…` (form validation) |
| [`tests/edge-case-cases.md`](tests/edge-case-cases.md) | API edge cases `E-01…` (naked short, close-to-zero, flip, fractional precision, invalid → 400) |
| [`tests/e2e-test-cases.md`](tests/e2e-test-cases.md) | End-to-end scenarios `T-01…` with step-by-step actions and expected UI |
| [`tests/traceability-matrix.md`](tests/traceability-matrix.md) | Every requirement mapped to the test-case IDs that cover it |

Each case has a stable ID, explicit inputs, and exact expected results, so it can be executed by hand or used to author automated tests.

---

## 10. API reference

| Method | Endpoint | Description | Success | Errors |
|---|---|---|---|---|
| `POST` | `/trades` | Book a new trade | `201 Created` + the created trade (with `notional`) | `400` problem-details on validation failure |
| `GET` | `/trades` | All trades, newest first | `200` | — |
| `GET` | `/positions` | Derived positions per symbol (net-zero omitted) | `200` | — |

**Request body for `POST /trades`:**
```json
{ "symbol": "AAPL", "side": "Buy", "quantity": 100, "price": 230.50 }
```
Validation: `symbol` non-empty; `side` ∈ {`Buy`, `Sell`}; `quantity` > 0; `price` > 0. Symbols are upper-cased; `id` and UTC `timestamp` are server-assigned.

## 11. Assumptions & Design Decisions

### Scope assumption: proof-of-concept, not production scale

This application is built as a **proof of concept**, not a production, high-throughput trading system. That assumption drives several deliberate simplifications:

- **Persistence (SQLite).** SQLite is used because it is zero-setup, cross-platform, and perfect for demonstrating the domain and the full stack. **For a real production deployment, the storage choice depends on the expected concurrency and the blotter's design**, and would change:
  - We would first need a **system-design parameter — how many concurrent users / trades-per-second** the blotter must sustain — to size the datastore correctly.
  - For durable, concurrent, multi-user trade storage I would move to a **proper RDBMS such as PostgreSQL** (self-hosted or managed, e.g. Supabase) instead of SQLite.
  - Depending on the blotter's read/latency profile, I would layer an **in-memory store such as Redis** for hot state (the live blotter feed and derived positions) so reads don't hit the primary DB on every tick.
- **No authentication / authorization** — out of scope for the PoC; a production system would add identity, per-desk entitlements, and audit.
- **Single-node, ephemeral** — no volume/replication; a production system would need durability, backups, and horizontal scale behind the concurrency numbers above.

### Domain & implementation decisions

- **Positions are derived, never stored** — the trade log is the single source of truth; `PositionCalculator` recomputes on every read. (At scale this is where a Redis-cached projection would help.)
- **Average cost reflects the open position only** — realized/unrealized P&L is intentionally out of scope for v1.
- **Naked shorts allowed** — selling with no prior position opens a negative (short) position.
- **Exact decimals** — money and quantities use `decimal` end-to-end; SQLite stores them as TEXT to preserve precision.
- **SQLite `ORDER BY` caveat** — SQLite cannot order by `DateTimeOffset`, so `GET /trades` sorts newest-first in memory (fine at PoC volumes; a real DB would index and sort server-side).
- **.NET 10** is targeted to match the installed SDK.

## 12. Given More Time

**Production-grade deployment & operations:**
- **Deploy on a production-level cloud** such as **Microsoft Azure** or **AWS** (managed compute, managed database, autoscaling, CI/CD) rather than a single-node demo host.
- **Clarify the concurrency requirement first** — ask how many concurrent users / trades-per-second the blotter must sustain — and **design the system accordingly** (datastore sizing, caching layer, partitioning, back-pressure). See the [Assumptions](#11-assumptions--design-decisions) section for the SQLite → PostgreSQL/Supabase + Redis reasoning.
- **Observability** — add richer structured **logging and monitoring**, and instrument the system with **OpenTelemetry** (distributed traces, metrics, and logs) feeding a backend like Azure Monitor / AWS CloudWatch / Grafana, with dashboards and alerting.

**Product & engineering enhancements:**
- Realized & unrealized **P&L** per position (with a mark price).
- **Filtering / search** by symbol and side in the blotter.
- **Live updates** via SignalR/WebSockets for multi-client sync.
- **EF Core migrations** instead of `EnsureCreated`; resolve the transitive `SQLitePCLRaw` advisory (NU1903).
- **Authentication & authorization**, pagination, and broader frontend component-test coverage.

---

<sub>See [`business-requirements.md`](business-requirements.md), [`project-architecture.md`](project-architecture.md), and [`cloud-session.md`](cloud-session.md) for requirements, architecture, and the development session log.</sub>
