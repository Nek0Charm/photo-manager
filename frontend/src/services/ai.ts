import http from './http'
import type { AiSettings, UpdateAiSettingsPayload } from '../types/ai'

export function fetchAiSettings() {
  return http.get<AiSettings>('/ai-settings').then((res) => res.data)
}

export function saveAiSettings(payload: UpdateAiSettingsPayload) {
  return http.post<AiSettings>('/ai-settings', payload).then((res) => res.data)
}
