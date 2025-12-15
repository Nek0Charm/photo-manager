import axios from 'axios'

const FALLBACK_API = 'http://localhost:5151/api'
const FALLBACK_DEV = 'http://localhost:5173'

const parseUrl = (value: string, base?: string) => {
  const trimmed = value.trim()
  const defaultBase = base ?? (typeof window !== 'undefined' ? window.location.origin : FALLBACK_DEV)
  try {
    return trimmed.startsWith('http') ? new URL(trimmed) : new URL(trimmed, defaultBase)
  } catch {
    return new URL(FALLBACK_API)
  }
}

const apiEnv = (import.meta.env.VITE_API_BASE_URL as string | undefined)?.trim()
const assetEnv = (import.meta.env.VITE_ASSET_BASE_URL as string | undefined)?.trim()

const apiUrl = apiEnv && apiEnv.length > 0 ? parseUrl(apiEnv) : new URL(FALLBACK_API)
const normalizedBase = apiUrl.href.replace(/\/$/, '')

const derivedAssetBase = normalizedBase.replace(/\/api$/i, '')
const assetUrl = assetEnv && assetEnv.length > 0 ? parseUrl(assetEnv) : new URL(derivedAssetBase)
const normalizedAssetBase = assetUrl.href.replace(/\/$/, '')

export const API_BASE = normalizedBase
export const API_ASSET_BASE = normalizedAssetBase.length ? normalizedAssetBase : normalizedBase

const http = axios.create({
  baseURL: API_BASE,
  withCredentials: true,
  timeout: 15000,
  headers: {
    Accept: 'application/json',
  },
})

export default http
