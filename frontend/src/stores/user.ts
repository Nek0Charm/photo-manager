import { defineStore } from 'pinia'
import type { User } from '../types/user'
import type { LoginPayload, RegisterPayload } from '../services/auth'
import { getCurrentUser, login as loginApi, logout as logoutApi, register as registerApi } from '../services/auth'
import { getErrorMessage } from '../utils/errors'
import { useAiSettingsStore } from './aiSettings'

export const useUserStore = defineStore('user', {
  state: () => ({
    user: null as User | null,
    loading: false,
    authChecked: false,
    error: '',
  }),
  getters: {
    isAuthenticated: (state) => !!state.user,
  },
  actions: {
    async fetchCurrentUser() {
      this.loading = true
      try {
        this.user = await getCurrentUser()
      } catch {
        this.user = null
      } finally {
        this.loading = false
        this.authChecked = true
      }
    },
    async login(payload: LoginPayload) {
      this.loading = true
      this.error = ''
      try {
        this.user = await loginApi(payload)
        this.authChecked = true
      } catch (error) {
        this.error = getErrorMessage(error, '登录失败')
        throw error
      } finally {
        this.loading = false
      }
    },
    async register(payload: RegisterPayload) {
      this.loading = true
      this.error = ''
      try {
        await registerApi(payload)
        this.user = await loginApi({ username: payload.username, password: payload.password })
        this.authChecked = true
      } catch (error) {
        this.error = getErrorMessage(error, '注册失败')
        throw error
      } finally {
        this.loading = false
      }
    },
    async logout() {
      await logoutApi()
      this.user = null
      useAiSettingsStore().reset()
    },
  },
})
