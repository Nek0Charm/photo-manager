import axios from 'axios'

const FALLBACK_ORIGIN = 'http://localhost:5173'
const resolveOrigin = () =>
  (typeof window !== 'undefined' && window.location?.origin) || FALLBACK_ORIGIN
const ORIGIN = resolveOrigin().replace(/\/$/, '')

export const API_BASE = `${ORIGIN}/api`
export const API_ASSET_BASE = ORIGIN

const http = axios.create({
  baseURL: '/api',
  withCredentials: true,
  timeout: 15000,
  headers: {
    Accept: 'application/json',
  },
})

export default http
