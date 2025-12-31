import axios from 'axios'

const pickString = (value: unknown) => (typeof value === 'string' ? value.trim() : '')

export function getErrorMessage(error: unknown, fallback: string = '请求失败'): string {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data
    if (typeof data === 'string') {
      const message = data.trim()
      if (message) return message
    } else if (data && typeof data === 'object') {
      const candidates = ['message', 'error', 'detail', 'title']
        .map((key) => pickString((data as Record<string, unknown>)[key]))
        .filter(Boolean)
      if (candidates.length) return candidates[0]!

      const errors = (data as { errors?: Record<string, unknown> }).errors
      if (errors && typeof errors === 'object') {
        const fromArray = Object.values(errors)
          .flat()
          .map((value) => pickString(value))
          .filter(Boolean)
        if (fromArray.length) return fromArray.join('；')
      }
    }

    const alt = pickString(error.response?.statusText) || pickString(error.message)
    if (alt) return alt
  } else if (error instanceof Error) {
    return error.message
  } else if (typeof error === 'string') {
    return error
  }

  return fallback
}
