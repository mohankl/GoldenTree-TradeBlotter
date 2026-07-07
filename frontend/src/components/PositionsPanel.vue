<script setup lang="ts">
import { computed } from 'vue'
import { useBlotterStore } from '../stores/blotter'
import { currency, qtyFmt, compactUsd } from '../format'

const store = useBlotterStore()

const sortedPositions = computed(() =>
  [...store.positions].sort((a, b) => a.symbol.localeCompare(b.symbol)),
)

function costBasis(netQuantity: number, averageCost: number): number {
  return Math.abs(netQuantity) * averageCost
}

// Largest cost basis in the book, used to scale each exposure bar (min 1 to avoid /0).
const maxBasis = computed(() =>
  Math.max(1, ...store.positions.map((p) => costBasis(p.netQuantity, p.averageCost))),
)

function exposureFill(netQuantity: number, averageCost: number): number {
  return Math.round((costBasis(netQuantity, averageCost) / maxBasis.value) * 100)
}
</script>

<template>
  <aside class="panel positions">
    <header class="panel__header">
      <span class="panel__title">Positions</span>
      <span class="panel__meta">{{ store.positions.length }} symbols</span>
    </header>

    <div class="pos-list">
      <div
        v-for="pos in sortedPositions"
        :key="pos.symbol"
        class="pos"
        data-testid="position-row"
        :data-symbol="pos.symbol"
      >
        <div class="pos__top">
          <span class="pos__sym">{{ pos.symbol }}</span>
          <span class="chip" :class="pos.netQuantity >= 0 ? 'chip--long' : 'chip--short'">
            {{ pos.netQuantity >= 0 ? 'LONG' : 'SHORT' }}
          </span>
        </div>

        <div class="pos__grid">
          <div class="metric">
            <span class="metric__l">Net Qty</span>
            <span
              class="metric__v num"
              :class="pos.netQuantity >= 0 ? 'is-long' : 'is-short'"
              data-testid="position-qty"
            >
              {{ pos.netQuantity > 0 ? '+' : '' }}{{ qtyFmt.format(pos.netQuantity) }}
            </span>
          </div>
          <div class="metric">
            <span class="metric__l">Avg Cost</span>
            <span class="metric__v num" data-testid="position-avg">{{ currency.format(pos.averageCost) }}</span>
          </div>
          <div class="metric">
            <span class="metric__l">Basis</span>
            <span class="metric__v num">{{ compactUsd(costBasis(pos.netQuantity, pos.averageCost)) }}</span>
          </div>
        </div>

        <div class="expo" :class="pos.netQuantity >= 0 ? 'expo--long' : 'expo--short'">
          <i :style="{ width: exposureFill(pos.netQuantity, pos.averageCost) + '%' }" />
        </div>
      </div>

      <p v-if="!store.loading && store.positions.length === 0" class="empty">No open positions.</p>
    </div>
  </aside>
</template>

<style scoped>
.positions {
  align-self: start;
}

.pos-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
  padding: 14px;
  max-height: 72vh;
  overflow: auto;
}

.pos {
  background: var(--surface);
  border: 1px solid var(--line);
  border-radius: var(--r-sm);
  padding: 12px 13px;
}

.pos__top {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 10px;
}

.pos__sym {
  font-weight: 700;
  font-size: 15px;
  letter-spacing: 0.02em;
}

.chip {
  font-family: var(--mono);
  font-size: 9.5px;
  font-weight: 700;
  letter-spacing: 0.1em;
  padding: 2px 7px;
  border-radius: 4px;
}

.chip--long {
  color: var(--buy);
  background: var(--buy-dim);
}

.chip--short {
  color: var(--sell);
  background: var(--sell-dim);
}

.pos__grid {
  display: grid;
  grid-template-columns: 1fr 1fr 1fr;
  gap: 8px;
  margin-bottom: 11px;
}

.metric {
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
}

.metric__l {
  font-family: var(--mono);
  font-size: 9px;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--faint);
}

.metric__v {
  font-size: 13.5px;
}

.metric__v.is-long {
  color: var(--buy);
}

.metric__v.is-short {
  color: var(--sell);
}

.expo {
  height: 5px;
  border-radius: 3px;
  background: var(--bg);
  overflow: hidden;
}

.expo > i {
  display: block;
  height: 100%;
  border-radius: 3px;
}

.expo--long > i {
  background: linear-gradient(90deg, var(--forest), var(--buy));
}

.expo--short > i {
  background: linear-gradient(90deg, var(--sell-dim), var(--sell));
}

.empty {
  margin: 0;
  padding: 16px 0;
  text-align: center;
  color: var(--faint);
  font-size: 13px;
}
</style>
