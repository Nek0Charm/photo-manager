<script setup lang="ts">
import { computed, ref } from 'vue'
import { useUserStore } from '../../stores/user'
import { usePhotoStore } from '../../stores/photos'
import { getErrorMessage } from '../../utils/errors'

const username = ref('')
const password = ref('')
const confirmPassword = ref('')
const email = ref('')
const mode = ref<'login' | 'register'>('login')
const errorMessage = ref('')
const showPassword = ref(false)

const userStore = useUserStore()
const photoStore = usePhotoStore()

const isRegister = computed(() => mode.value === 'register')
const actionLabel = computed(() => (isRegister.value ? '注册并登录' : '登录'))
const helperText = computed(() =>
  isRegister.value ? '请输入邮箱并设置密码，注册成功后将自动登录。' : '使用已注册的用户名或邮箱登录系统。',
)

const resetFeedback = () => {
  errorMessage.value = ''
}

const switchMode = (value: string | null) => {
  const next = value === 'register' ? 'register' : 'login'
  if (mode.value !== next) {
    mode.value = next
  }
  resetFeedback()
  if (next === 'login') {
    email.value = ''
    confirmPassword.value = ''
  }
}

type LoginFormPayload = { username: string; password: string }
type RegisterFormPayload = LoginFormPayload & { email: string }

const validate = (): LoginFormPayload | RegisterFormPayload | null => {
  const trimmedUsername = username.value.trim()
  const trimmedPassword = password.value
  if (!trimmedUsername) {
    errorMessage.value = '请输入用户名或邮箱'
    return null
  }
  if (!trimmedPassword) {
    errorMessage.value = '请输入密码'
    return null
  }

  if (isRegister.value) {
    const trimmedEmail = email.value.trim()
    if (!trimmedEmail) {
      errorMessage.value = '请输入邮箱'
      return null
    }
    const emailPattern = /.+@.+\..+/
    if (!emailPattern.test(trimmedEmail)) {
      errorMessage.value = '邮箱格式不正确'
      return null
    }
    if (trimmedPassword.length < 6) {
      errorMessage.value = '密码至少 6 个字符'
      return null
    }
    if (trimmedPassword !== confirmPassword.value) {
      errorMessage.value = '两次输入的密码不一致'
      return null
    }
    return { username: trimmedUsername, password: trimmedPassword, email: trimmedEmail }
  }

  return { username: trimmedUsername, password: trimmedPassword }
}

const submit = async () => {
  resetFeedback()
  const payload = validate()
  if (!payload) return

  try {
    if (isRegister.value && 'email' in payload) {
      await userStore.register(payload)
    } else {
      await userStore.login(payload)
    }
    await photoStore.fetchPhotos()
  } catch (error) {
    errorMessage.value = getErrorMessage(
      error,
      userStore.error || (isRegister.value ? '注册失败' : '登录失败'),
    )
  }
}
</script>

<template>
  <div class="login-shell">
    <v-card class="login-card" rounded="xl" elevation="8">
      <v-tabs v-model="mode" class="login-tabs" grow color="primary" @update:modelValue="switchMode">
        <v-tab value="login">登录</v-tab>
        <v-tab value="register">注册</v-tab>
      </v-tabs>
      <v-card-title class="login-title text-h5 font-weight-bold">Photo Manager</v-card-title>
      <v-card-subtitle class="login-subtitle text-body-2 text-medium-emphasis">{{ helperText }}</v-card-subtitle>
      <v-card-text class="pt-0">
        <v-text-field
          v-model="username"
          :label="isRegister ? '用户名' : '用户名 / 邮箱'"
          variant="outlined"
          hide-details
          class="mb-4"
          @keydown.enter="submit"
        />
        <v-text-field
          v-if="isRegister"
          v-model="email"
          label="联系邮箱"
          variant="outlined"
          hide-details
          class="mb-4"
          @keydown.enter="submit"
        />
        <v-text-field
          v-model="password"
          :type="showPassword ? 'text' : 'password'"
          label="密码"
          variant="outlined"
          hide-details
          :append-inner-icon="showPassword ? 'mdi-eye-off' : 'mdi-eye'"
          class="mb-4"
          @click:append-inner="showPassword = !showPassword"
          @keydown.enter="submit"
        />
        <v-text-field
          v-if="isRegister"
          v-model="confirmPassword"
          type="password"
          label="确认密码"
          variant="outlined"
          hide-details
          class="mb-4"
          @keydown.enter="submit"
        />
        <v-alert v-if="errorMessage" type="error" variant="tonal" class="mb-4" density="comfortable">
          {{ errorMessage }}
        </v-alert>
      </v-card-text>
      <v-card-actions class="pa-0 pb-4">
        <v-btn color="primary" block :loading="userStore.loading" @click="submit">
          {{ actionLabel }}
        </v-btn>
      </v-card-actions>
    </v-card>
  </div>
</template>

<style scoped>
.login-shell {
  min-height: calc(100vh - 120px);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 32px 16px;
}

.login-card {
  width: min(480px, 100%);
  padding-inline: 8px;
}

.login-tabs {
  border-bottom: 1px solid var(--pm-border-subtle, rgba(0, 0, 0, 0.08));
}

.login-title {
  text-align: center;
  line-height: 1.4;
  white-space: normal;
  word-break: break-word;
}

.login-subtitle {
  text-align: center;
  white-space: normal;
  word-break: break-word;
  margin-bottom: 16px;
}
</style>
