# GoldenTree Trade Blotter — Brand Guidelines

A compact brand system for the **GoldenTree Trade Blotter** web app: a dark,
professional trading terminal with a refined gold accent and deep-green
secondary, balanced by clear Buy/Sell signal colors.

---

## Source & inspiration

These assets are **original works** created for this project, visually inspired
by the refined, institutional aesthetic of **GoldenTree Asset Management**
(<https://www.goldentree.com/>). They are **not** the firm's actual trademarked
logo, wordmark, or brand files, and no copyrighted assets were downloaded or
embedded.

The public site presents as a minimalist, corporate asset-management brand:
generous negative space, a clean sans-serif type system, restrained dark
neutrals, and gold undertones implied by the name. No explicit hex values were
exposed on the page at time of research, so the palette below is a **sensible,
original approximation** — a refined gold + deep forest green + dark trading
neutrals — chosen to match that aesthetic and to serve a trading UI.

---

## Color palette

### Brand

| Token | Hex | Usage |
|-------|-----|-------|
| `--gt-gold`        | `#C9A227` | **Primary brand gold.** Logo, primary buttons, key accents. |
| `--gt-gold-bright` | `#D4AF37` | Brighter gold for hovers, highlights, emphasis. |
| `--gt-gold-soft`   | `#E6C868` | Soft gold for subtle fills and focus rings. |
| `--gt-gold-deep`   | `#9A7B1C` | Deep gold for pressed states / borders on gold surfaces. |
| `--gt-green`       | `#0F3D2E` | **Secondary brand** — deep forest green (tiles, brand chrome). |
| `--gt-green-mid`   | `#1B5E43` | Mid green for secondary surfaces and tints. |
| `--gt-green-bright`| `#2E8B63` | Brighter green for links / emphasis on dark. |

### Semantic — Buy / Sell

| Token | Hex | Usage |
|-------|-----|-------|
| `--gt-buy`        | `#22C55E` | **BUY** side — direction badges, positive P&L. |
| `--gt-buy-strong` | `#16A34A` | BUY pressed / strong emphasis. |
| `--gt-buy-soft`   | `rgba(34,197,94,.14)` | BUY row / tag background tint. |
| `--gt-sell`       | `#EF4444` | **SELL** side — direction badges, negative P&L. |
| `--gt-sell-strong`| `#DC2626` | SELL pressed / strong emphasis. |
| `--gt-sell-soft`  | `rgba(239,68,68,.14)` | SELL row / tag background tint. |

> Note: keep Buy/Sell green distinct from the deep brand green. Brand green is
> chrome; `--gt-buy` is a signal. Never use brand green to mean "Buy".

### Status

| Token | Hex | Usage |
|-------|-----|-------|
| `--gt-info`    | `#38BDF8` | Informational / neutral status. |
| `--gt-warning` | `#F59E0B` | Warning / pending fills. |

### Dark-theme neutrals (trading terminal)

| Token | Hex | Usage |
|-------|-----|-------|
| `--gt-bg`            | `#0B0F14` | App background — near-black slate. |
| `--gt-bg-elevated`   | `#0F141B` | Slightly raised background regions. |
| `--gt-surface`       | `#131A22` | Cards, panels, table body. |
| `--gt-surface-2`     | `#1B2530` | Elevated surface — headers, menus, hover. |
| `--gt-surface-3`     | `#24303D` | Highest surface — popovers, active rows. |
| `--gt-border`        | `#263241` | Default borders / dividers. |
| `--gt-border-strong` | `#34455A` | Emphasized borders, table gridlines. |
| `--gt-text`          | `#E6EAF0` | Primary text. |
| `--gt-text-muted`    | `#9AA7B4` | Secondary / muted text. |
| `--gt-text-faint`    | `#64748B` | Disabled / placeholder text. |
| `--gt-text-on-gold`  | `#0B0F14` | Text on a gold fill (dark, for contrast). |
| `--gt-text-on-green` | `#E9F5EE` | Text on a deep-green fill. |

All tokens are defined in [`palette.css`](./palette.css).

---

## Typography

Use a **system font stack** — no web-font dependency, fast, and native to the
trading desk OS:

```css
font-family: 'Inter', 'Helvetica Neue', Helvetica, Arial, system-ui,
             -apple-system, 'Segoe UI', Roboto, sans-serif;
```

- **Wordmark / headings:** weight 700, tight letter-spacing (~0.2px).
- **Body / UI:** weight 400–500.
- **Numeric data (prices, quantities, P&L):** use a tabular / monospaced feel
  for column alignment, e.g. `font-variant-numeric: tabular-nums;` or a mono
  stack: `'SF Mono','JetBrains Mono',ui-monospace,Menlo,Consolas,monospace`.

The wordmark in `logo.svg` uses this system stack via `font-family` and
therefore **depends on the host system fonts** (Inter if present, otherwise the
next available sans-serif). This keeps the SVG self-contained with no embedded
fonts. If pixel-perfect rendering across all machines is required, convert the
wordmark text to paths.

---

## Logo usage

- **`logo.svg`** — full horizontal wordmark (glyph + "GoldenTree"). Use in the
  app header, login screen, and about panel. Designed for **dark backgrounds**.
- **`logo-mark.svg`** — glyph only (layered golden tree). Use where the wordmark
  is too wide: collapsed sidebars, compact headers, loading states.
- **`favicon.svg`** — bold, simplified mark on a green tile; reads at 16–32px.
- **`icon-192.svg` / `icon-512.svg`** — PWA / installable app icons: the mark on
  a branded deep-green tile with a subtle gold hairline.

Guidelines:
- Maintain clear space around the logo equal to the height of one canopy tier.
- Do **not** recolor the gold to a flat/neon tone, stretch, or add drop shadows.
- On light surfaces (rare), place the mark inside the green tile rather than
  putting bare gold on white.
- Minimum wordmark width ~160px; below that, switch to `logo-mark.svg`.

---

## Iconography style

- **Grid:** 24×24 viewBox, centered, ~2px visual padding.
- **Stroke:** 1.5–2px, `stroke-linecap="round"`, `stroke-linejoin="round"`.
- **Color:** `currentColor` for neutral UI icons so they inherit text color;
  Buy/Sell icons are intentionally hard-coded green/red for unambiguous signal.
- **Feel:** geometric, minimal, single-weight — matching the refined brand.
- Set: `buy`, `sell`, `trade`, `positions`, `plus` (see [`icons/`](./icons/)).

The tree mark itself is built from three stacked canopy tiers — reading as both
a **tree** (the brand) and an **upward trend** (growth / a rising market), a
quiet nod to the trading context.
