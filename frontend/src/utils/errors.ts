import axios from 'axios'

export function getErrorMessage(error: unknown, fallback = '请求失败') {
  if (axios.isAxiosError(error)) {
    const dataMessage = (error.response?.data as { message?: string } | undefined)?.message
    if (dataMessage) return dataMessage
    if (error.message) return error.message
  } else if (error instanceof Error) {
    return error.message
  } else if (typeof error === 'string') {
    return error
  }

  return fallback
}
