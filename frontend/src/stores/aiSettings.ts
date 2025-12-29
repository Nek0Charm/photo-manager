import { defineStore } from 'pinia'
import type { AiSettings, UpdateAiSettingsPayload } from '../types/ai'
import { fetchAiSettings, saveAiSettings } from '../services/ai'
import { getErrorMessage } from '../utils/errors'

export const useAiSettingsStore = defineStore('ai-settings', {
  state: () => ({
    data: null as AiSettings | null,
    loading: false,
    saving: false,
    error: '',
  }),
  actions: {
    async fetchSettings() {
      this.loading = true
      this.error = ''
      try {
        this.data = await fetchAiSettings()
      } catch (error) {
        this.error = getErrorMessage(error, '获取 AI 配置失败')
        throw error
      } finally {
        this.loading = false
      }
    },
    async saveSettings(payload: UpdateAiSettingsPayload) {
      this.saving = true
      this.error = ''
      try {
        const result = await saveAiSettings(payload)
        this.data = result
        return result
      } catch (error) {
        this.error = getErrorMessage(error, '保存 AI 配置失败')
        throw error
      } finally {
        this.saving = false
      }
    },
    reset() {
      this.data = null
      this.loading = false
      this.saving = false
      this.error = ''
    },
  },
})
