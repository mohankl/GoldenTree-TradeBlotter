import { defineStore } from 'pinia'
import { ref } from 'vue'
import { api, ApiError } from '../api/client'
import type { Trade, Position, TradeInput } from '../types'

/**
 * Single source of truth for the blotter. Owns all server communication so
 * components stay presentational. After a successful trade submission, both the
 * trade list and the derived positions are refreshed, keeping every panel
 * reactive with no page reload.
 */
export const useBlotterStore = defineStore('blotter', () => {
  const trades = ref<Trade[]>([])
  const positions = ref<Position[]>([])
  const loading = ref(false)
  const submitting = ref(false)
  const error = ref<string | null>(null)

  /** Load trades and positions together (used on mount). */
  async function fetchAll() {
    loading.value = true
    error.value = null
    try {
      const [t, p] = await Promise.all([api.getTrades(), api.getPositions()])
      trades.value = t
      positions.value = p
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Failed to load blotter data.'
    } finally {
      loading.value = false
    }
  }

  /**
   * Submit a new trade. On success, refreshes trades + positions and returns the
   * created trade. On a validation failure, re-throws the ApiError so the form can
   * surface field-level messages.
   */
  async function submitTrade(input: TradeInput): Promise<Trade> {
    submitting.value = true
    error.value = null
    try {
      const created = await api.createTrade(input)
      // Refresh derived state so blotter + positions update immediately.
      const [t, p] = await Promise.all([api.getTrades(), api.getPositions()])
      trades.value = t
      positions.value = p
      return created
    } catch (e) {
      if (!(e instanceof ApiError)) {
        error.value = e instanceof Error ? e.message : 'Failed to submit trade.'
      }
      throw e
    } finally {
      submitting.value = false
    }
  }

  return { trades, positions, loading, submitting, error, fetchAll, submitTrade }
})
