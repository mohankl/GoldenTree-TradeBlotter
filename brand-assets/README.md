# brand-assets/

Brand assets and icons for the **GoldenTree Trade Blotter** web app. All art is
**original**, created for this project and visually inspired by — not copied
from — GoldenTree Asset Management's aesthetic. See
[`brand-guidelines.md`](./brand-guidelines.md) for the full system and the
"source & inspiration" note.

## Contents

| File | What it is |
|------|-----------|
| `brand-guidelines.md` | Full brand system: palette, typography, logo & icon rules. |
| `palette.css` | CSS custom properties (`--gt-*`) — the design tokens. |
| `logo.svg` | Horizontal wordmark (glyph + "GoldenTree"), for the header. |
| `logo-mark.svg` | Glyph only (layered golden tree), for compact spots. |
| `favicon.svg` | Bold simplified mark for 16–32px browser tabs. |
| `icon-192.svg` | PWA app icon, 192px, mark on a green tile. |
| `icon-512.svg` | PWA app icon, 512px, mark on a green tile. |
| `icons/buy.svg` | Up arrow, green — Buy side. |
| `icons/sell.svg` | Down arrow, red — Sell side. |
| `icons/trade.svg` | Exchange arrows — a trade (`currentColor`). |
| `icons/positions.svg` | Bar chart — positions / portfolio (`currentColor`). |
| `icons/plus.svg` | Plus — add trade (`currentColor`). |

## How the frontend should consume it

1. **Palette** — import the tokens once, globally (e.g. in `main.ts`/`main.js`
   or your root stylesheet), then use `var(--gt-*)` everywhere:
   ```css
   @import "../brand-assets/palette.css";
   ```
   (Adjust the relative path to where the file sits relative to your Vue app.)

2. **Header logo** — inline or `<img>` the wordmark:
   ```html
   <img src="/brand-assets/logo.svg" alt="GoldenTree Trade Blotter" height="32" />
   ```
   Use `logo-mark.svg` in collapsed / narrow layouts.

3. **Favicon + PWA** — in `index.html` / web manifest:
   ```html
   <link rel="icon" type="image/svg+xml" href="/brand-assets/favicon.svg" />
   ```
   ```json
   { "icons": [
       { "src": "/brand-assets/icon-192.svg", "sizes": "192x192", "type": "image/svg+xml" },
       { "src": "/brand-assets/icon-512.svg", "sizes": "512x512", "type": "image/svg+xml" }
   ] }
   ```

4. **UI icons** — inline the SVGs (so `currentColor` and `stroke` inherit from
   your CSS), or reference them directly. Buy/Sell icons carry their own
   green/red; neutral icons follow the surrounding text color.

## Color swatches

| Swatch | Token | Hex |
|--------|-------|-----|
| 🟡 | `--gt-gold` | `#C9A227` |
| 🟡 | `--gt-gold-bright` | `#D4AF37` |
| 🟢 | `--gt-green` | `#0F3D2E` |
| 🟢 | `--gt-green-mid` | `#1B5E43` |
| 🟩 | `--gt-buy` | `#22C55E` |
| 🟥 | `--gt-sell` | `#EF4444` |
| ⬛ | `--gt-bg` | `#0B0F14` |
| ⬛ | `--gt-surface` | `#131A22` |
| ⬛ | `--gt-surface-2` | `#1B2530` |
| ▪️ | `--gt-border` | `#263241` |
| ⬜ | `--gt-text` | `#E6EAF0` |
| ▫️ | `--gt-text-muted` | `#9AA7B4` |

Full token list with usage notes: [`brand-guidelines.md`](./brand-guidelines.md).
