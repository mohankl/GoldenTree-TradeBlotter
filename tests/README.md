# Trade Blotter — Test-Case Catalog

> QA artifact for the **GoldenTree Trade Blotter** (.NET 10 REST API + Vue 3 / Pinia / Vite SPA).
> This folder is **documentation only** — it enumerates every test case across the three test layers so a
> reviewer can trace **requirements → tests**. It mirrors the terminology and rules in the two
> authoritative source docs:
> - [`../business-requirements.md`](../business-requirements.md) — the *what/why*.
> - [`../project-architecture.md`](../project-architecture.md) — the *how*.

**Last updated:** 2026-07-07.

---

## 1. What we are testing

A full-stack trade blotter. A user enters trades; a live blotter table shows every trade **newest-first**;
a positions panel shows the **net position** and **average cost** per symbol, **derived from trade history
and never stored**. The explicitly graded, critical piece is **average-cost behavior on mixed buys/sells**,
including the **flip-through-zero reset**.

### System-under-test contract (quick reference)

| Method | Path | Success | Notes |
|---|---|---|---|
| `POST` | `/trades` | **201** + created trade | Body `{ symbol, side (Buy\|Sell), quantity, price }`. Response includes `notional = quantity × price`; server assigns `id` + UTC `timestamp`; `symbol` upper-cased. Invalid → **400** RFC 7807 problem-details. |
| `GET` | `/trades` | **200** | All trades **newest first**, each with `notional`. |
| `GET` | `/positions` | **200** | One entry per symbol: `netQuantity` (signed; + long / − short) and `averageCost`. **Net-zero symbols omitted.** |

**Validation rules:** `symbol` non-empty; `side ∈ {Buy, Sell}`; `quantity > 0`; `price > 0`.

### Position / average-cost rules (replay each symbol oldest→newest; Buy = +qty, Sell = −qty)

1. **Open from flat** → `avg = trade price`.
2. **Increase exposure** (same direction) → weighted avg: `avg = (avg·|net| + price·|qty|) / (|net| + |qty|)`.
3. **Reduce exposure without crossing zero** → `avg` **unchanged**; net moves toward 0; exactly 0 ⇒ flat/omitted.
4. **Flip through zero** (opposite side, larger than net) → `avg` **resets** to the flipping trade's price.

**Canonical worked example (referenced throughout):**
`Buy 100 @10, Buy 100 @12 → 200 @11`; `Sell 50 @15 → 150 @11` (avg unchanged, rule 3);
`Sell 200 @20 → short 50 @20` (flip reset, rule 4).

---

## 2. Test strategy — three layers

| Layer | Focus | Tooling | Location | Run |
|---|---|---|---|---|
| **Unit** | Pure position-calculation logic (`PositionCalculator.Derive`) + frontend form-validation logic | **xUnit** (backend) and **Vitest + Vue Test Utils** (frontend) | `backend/TradeBlotter.Tests` and `frontend/src/components/__tests__` | `dotnet test` / `npm run test:unit` |
| **Edge** | End-to-end at the **API level** — boundary/critical behaviors over the real HTTP path against a temp/in-memory SQLite DB | **xUnit** + `WebApplicationFactory` | `backend/TradeBlotter.Tests` | `dotnet test` |
| **End-to-End** | Real Vue app **browser ↔ real .NET API** — user-visible behavior | **Playwright** | `frontend/e2e` | `npm run test:e2e` |

- **[`unit-test-cases.md`](./unit-test-cases.md)** — `U-##` position-logic cases + `U-F##` frontend form-validation cases.
- **[`edge-case-cases.md`](./edge-case-cases.md)** — `E-##` API-level edge cases (concrete request bodies + status/response assertions).
- **[`e2e-test-cases.md`](./e2e-test-cases.md)** — `T-##` Playwright front-to-back scenarios (user steps + UI assertions).
- **[`traceability-matrix.md`](./traceability-matrix.md)** — requirement → test-case-ID coverage map.

### How to run each suite

```bash
# Backend: unit (position logic) + edge (API integration) — layers Unit(backend) & Edge
cd backend
dotnet test

# Frontend: form-validation unit tests — layer Unit(frontend)
cd frontend
npm run test:unit

# Frontend: Playwright end-to-end — layer End-to-End
#   (runs the real Vue app against the real .NET API; SQLite pointed at a temp/test DB)
cd frontend
npm run test:e2e
```

---

## 3. Legend for the test-case tables

Every case table in this catalog uses these columns:

| Column | Meaning |
|---|---|
| **ID** | Stable, unique identifier. Prefixes: `U-` unit (position logic), `U-F` unit (frontend form), `E-` edge (API), `T-` end-to-end (Playwright). |
| **Title** | Short description of the behavior under test. |
| **Preconditions** | State/setup that must hold before the steps run (e.g. clean DB, app loaded). |
| **Input / Steps** | Exact trade sequences, request bodies, or ordered user actions. Numbers are concrete so a human or a test author can execute them verbatim. |
| **Expected Result** | The precise, assertable outcome (net qty, avg cost, HTTP status, DOM state). |
| **Requirement ref** | Back-reference to the source docs — `BR §n` = `business-requirements.md`, `ARCH §…` = `project-architecture.md`, or a named rule (Rule 1–4). |

**ID prefix summary**

| Prefix | Layer | File |
|---|---|---|
| `U-##` | Unit — position calculation | `unit-test-cases.md` |
| `U-F##` | Unit — frontend form validation | `unit-test-cases.md` |
| `E-##` | Edge — API integration | `edge-case-cases.md` |
| `T-##` | End-to-End — Playwright | `e2e-test-cases.md` |

---

## 4. Case inventory (summary)

| File | Layer | IDs | Count |
|---|---|---|---|
| `unit-test-cases.md` | Unit (position logic) | `U-01` … `U-10` | 10 |
| `unit-test-cases.md` | Unit (frontend form) | `U-F01` … `U-F06` | 6 |
| `edge-case-cases.md` | Edge (API) | `E-01` … `E-08` | 8 |
| `e2e-test-cases.md` | End-to-End (Playwright) | `T-01` … `T-06` | 6 |
| **Total** | | | **30** |

See [`traceability-matrix.md`](./traceability-matrix.md) for the full requirement-to-test mapping.
