export interface AiSettings {
  provider: string
  model: string
  endpoint: string | null
  hasApiKey: boolean
  updatedAt?: string | null
}

export interface UpdateAiSettingsPayload {
  model: string
  endpoint?: string | null
  apiKey?: string | null
  updateApiKey: boolean
}
