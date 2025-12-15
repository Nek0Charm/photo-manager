import http from './http'
import type { User } from '../types/user'

export interface LoginPayload {
  username: string
  password: string
}

export interface RegisterPayload extends LoginPayload {
  email: string
}

export function login(payload: LoginPayload) {
  return http.post<User>('/auth/login', payload).then((res) => res.data)
}

export function logout() {
  return http.post('/auth/logout')
}

export function getCurrentUser() {
  return http.get<User>('/auth/me').then((res) => res.data)
}

export function register(payload: RegisterPayload) {
  return http.post('/auth/register', payload)
}
