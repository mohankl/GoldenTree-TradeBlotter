<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useBlotterStore } from './stores/blotter'
import { compactUsd } from './format'
import TradeEntryForm from './components/TradeEntryForm.vue'
import BlotterTable from './components/BlotterTable.vue'
import PositionsPanel from './components/PositionsPanel.vue'

const store = useBlotterStore()

onMounted(() => {
  store.fetchAll()
})

// --- KPI summary (surface the summary before the detail) ---
const openPositions = computed(() => store.positions.length)
const tradeCount = computed(() => store.trades.length)

// Signed exposure per position = netQty * avgCost. Gross = sum of |exposure|.
const grossExposure = computed(() =>
  store.positions.reduce((sum, p) => sum + Math.abs(p.netQuantity) * p.averageCost, 0),
)
const netExposure = computed(() =>
  store.positions.reduce((sum, p) => sum + p.netQuantity * p.averageCost, 0),
)
const netBias = computed(() => {
  if (store.positions.length === 0) return 'FLAT'
  return netExposure.value >= 0 ? 'LONG' : 'SHORT'
})
</script>

<template>
  <div class="app">
    <header class="topbar">
      <div class="brand">
        <svg class="brand__emblem" viewBox="0 0 48 48" aria-hidden="true">
          <defs>
            <linearGradient id="gt-tile" x1="0" y1="0" x2="1" y2="1">
              <stop offset="0" stop-color="#1a1608" />
              <stop offset="1" stop-color="#0d0b05" />
            </linearGradient>
            <linearGradient id="gt-au" x1="0" y1="1" x2="0" y2="0">
              <stop offset="0" stop-color="#9a7b1c" />
              <stop offset="1" stop-color="#e3c15a" />
            </linearGradient>
          </defs>
          <rect x="1" y="1" width="46" height="46" rx="12" fill="url(#gt-tile)" stroke="#3a3016" />
          <path d="M24 9 L31 18 H17 Z" fill="url(#gt-au)" />
          <path d="M24 16 L33 27 H15 Z" fill="url(#gt-au)" opacity="0.92" />
          <path d="M24 23 L35.5 37 H12.5 Z" fill="url(#gt-au)" opacity="0.84" />
          <rect x="22.4" y="35" width="3.2" height="5.5" rx="1.2" fill="#12563b" />
        </svg>
        <div class="brand__text">
          <h1 class="brand__name"><b>GoldenTree</b></h1>
          <span class="brand__sub">Trade Blotter</span>
        </div>
      </div>

      <div class="kpis">
        <div class="kpi">
          <div class="kpi__label">Open Positions</div>
          <div class="kpi__value num" data-testid="kpi-positions">{{ openPositions }}</div>
        </div>
        <div class="kpi">
          <div class="kpi__label">Trades</div>
          <div class="kpi__value num" data-testid="kpi-trades">{{ tradeCount }}</div>
        </div>
        <div class="kpi">
          <div class="kpi__label">Gross Exposure</div>
          <div class="kpi__value num kpi__value--gold">{{ compactUsd(grossExposure) }}</div>
        </div>
        <div class="kpi">
          <div class="kpi__label">Net Bias</div>
          <div
            class="kpi__value num"
            :class="{ 'kpi__value--long': netBias === 'LONG', 'kpi__value--short': netBias === 'SHORT' }"
          >
            {{ netBias }}
          </div>
        </div>
      </div>
    </header>

    <div v-if="store.error" class="banner" data-testid="global-error">{{ store.error }}</div>

    <!-- Order ticket (horizontal trade entry) -->
    <TradeEntryForm />

    <!-- Body: blotter + positions rail -->
    <main class="body">
      <BlotterTable />
      <PositionsPanel />
    </main>
  </div>
</template>

<style scoped>
.app {
  max-width: 1240px;
  margin: 0 auto;
  padding: 18px 22px 44px;
}

.topbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 20px;
  padding: 6px 2px 18px;
  flex-wrap: wrap;
}

.brand {
  display: flex;
  align-items: center;
  gap: 13px;
}

.brand__emblem {
  width: 46px;
  height: 46px;
  flex: none;
}

.brand__name {
  font-size: 19px;
  font-weight: 650;
  letter-spacing: -0.01em;
}

.brand__name b {
  color: var(--gold);
  font-weight: 700;
}

.brand__sub {
  display: block;
  font-family: var(--mono);
  font-size: 10.5px;
  letter-spacing: 0.28em;
  text-transform: uppercase;
  color: var(--muted);
  margin-top: 1px;
}

.kpis {
  display: flex;
  gap: 10px;
  flex-wrap: wrap;
}

.kpi {
  background: linear-gradient(180deg, var(--surface), var(--panel));
  border: 1px solid var(--line);
  border-radius: var(--r-sm);
  padding: 8px 14px;
  min-width: 106px;
}

.kpi__label {
  font-family: var(--mono);
  font-size: 9.5px;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  color: var(--faint);
}

.kpi__value {
  font-size: 18px;
  font-weight: 600;
  margin-top: 2px;
  letter-spacing: -0.01em;
}

.kpi__value--gold {
  color: var(--gold-bright);
}
.kpi__value--long {
  color: var(--buy);
}
.kpi__value--short {
  color: var(--sell);
}

.banner {
  margin: 0 0 16px;
  font-size: 13px;
  color: var(--sell);
  background: var(--sell-dim);
  border: 1px solid var(--sell);
  padding: 8px 13px;
  border-radius: var(--r-sm);
}

.body {
  display: grid;
  grid-template-columns: 1fr 340px;
  gap: 18px;
  align-items: start;
}

@media (max-width: 940px) {
  .body {
    grid-template-columns: 1fr;
  }
}
</style>
