<script setup lang="ts">
import { computed, onBeforeUnmount, watch } from 'vue'
import { storeToRefs } from 'pinia'
import { useDisplay } from 'vuetify'
import logoMark from '../../assets/logo.svg'
import { useUiStore } from '../../stores/ui'
import { usePhotoStore } from '../../stores/photos'
import { useUserStore } from '../../stores/user'

const emit = defineEmits<{ (e: 'open-upload'): void; (e: 'open-ai-settings'): void }>()

const SEARCH_DEBOUNCE_MS = 500
let searchDebounceTimer: ReturnType<typeof setTimeout> | null = null

const uiStore = useUiStore()
const photoStore = usePhotoStore()
const userStore = useUserStore()
const { query } = storeToRefs(photoStore)
const { user } = storeToRefs(userStore)
const { smAndDown } = useDisplay()
const isMobile = computed(() => smAndDown.value)

const displayUsername = computed(() => {
  if (!user.value?.username) return ''
  const username = user.value.username.trim()
  return smAndDown.value ? username.charAt(0).toUpperCase() : username
})

const themeIcon = computed(() => (uiStore.currentTheme === 'lightTheme' ? 'mdi-weather-night' : 'mdi-white-balance-sunny'))
const themeLabel = computed(() => (uiStore.currentTheme === 'lightTheme' ? '切换到暗色模式' : '切换到亮色模式'))

const handleLogout = async () => {
  await userStore.logout()
  photoStore.reset()
}

const runSearch = () => {
  if (searchDebounceTimer) {
    clearTimeout(searchDebounceTimer)
    searchDebounceTimer = null
  }
  if (userStore.isAuthenticated) {
    photoStore.setPage(1)
    photoStore.fetchPhotos().catch(() => undefined)
  }
}

const scheduleDebouncedSearch = () => {
  if (!userStore.isAuthenticated) {
    return
  }
  if (searchDebounceTimer) {
    clearTimeout(searchDebounceTimer)
  }
  searchDebounceTimer = setTimeout(() => {
    searchDebounceTimer = null
    runSearch()
  }, SEARCH_DEBOUNCE_MS)
}

watch(query, (newValue, oldValue) => {
  if (newValue === oldValue) {
    return
  }
  scheduleDebouncedSearch()
})

onBeforeUnmount(() => {
  if (searchDebounceTimer) {
    clearTimeout(searchDebounceTimer)
  }
})
</script>

<template>
  <v-app-bar flat class="app-header" color="surface">
    <v-toolbar-title class="logo-title font-weight-bold text-h6 d-flex align-center">
      <img :src="logoMark" alt="Photo Manager logo" class="logo-mark" />
    </v-toolbar-title>
    <div v-if="!isMobile" class="search-field flex-grow-1">
      <v-text-field
        v-model="query"
        density="comfortable"
        variant="outlined"
        hide-details
        prepend-inner-icon="mdi-magnify"
        placeholder="搜索描述或标签"
        @keydown.enter.prevent="runSearch"
      />
    </div>
    <div class="header-actions" :class="{ 'header-actions--mobile': isMobile }">
      <v-btn icon :title="themeLabel" variant="text" @click="uiStore.toggleTheme">
        <v-icon :icon="themeIcon" />
      </v-btn>
      <v-menu v-if="user" transition="fade-transition" offset-y>
        <template #activator="{ props: menuProps }">
          <v-btn v-bind="menuProps" variant="tonal" prepend-icon="mdi-account" :title="user?.username || ''">
            {{ displayUsername }}
          </v-btn>
        </template>
        <v-list density="compact">
          <v-list-item title="AI 标签配置" prepend-icon="mdi-robot-outline" @click="emit('open-ai-settings')" />
          <v-divider class="my-1" />
          <v-list-item title="退出登录" prepend-icon="mdi-logout" @click="handleLogout" />
        </v-list>
      </v-menu>
      <v-btn
        class="quick-upload-btn"
        color="primary"
        prepend-icon="mdi-upload"
        :disabled="!userStore.isAuthenticated"
        aria-label="快速上传"
        title="快速上传"
        @click="emit('open-upload')"
      >
        <span class="quick-upload-label">快速上传</span>
      </v-btn>
    </div>
    <template v-if="isMobile" #extension>
      <div class="search-field search-field-mobile">
        <v-text-field
          v-model="query"
          density="comfortable"
          variant="outlined"
          hide-details
          prepend-inner-icon="mdi-magnify"
          placeholder="搜索描述或标签"
          @keydown.enter.prevent="runSearch"
        />
      </div>
    </template>
  </v-app-bar>
</template>

<style scoped>
.app-header {
  border-bottom: 1px solid var(--pm-border-subtle);
  padding-inline: 16px;
  min-height: 72px;
}

.app-header :deep(.v-toolbar__content) {
  width: 100%;
  display: flex;
  align-items: center;
  gap: 12px;
}

.header-actions {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-left: auto;
  flex: 0 0 auto;
}

.header-actions--mobile {
  margin-left: 0;
}

.search-field {
  display: flex;
  flex-direction: column;
  gap: 4px;
  max-width: 420px;
  flex: 1 1 260px;
  min-width: 160px;
  margin-left: 12px;
}

.search-field-mobile {
  margin: 8px 16px 16px;
  max-width: none;
  flex: 1 1 100%;
}

.search-field :deep(.v-field) {
  overflow: hidden;
}

.search-field :deep(.v-field__prepend-inner) {
  margin-inline-end: 6px;
}

.search-status {
  min-height: 18px;
}

.logo-title {
  gap: 14px;
  flex-shrink: 0;
  min-width: auto;
  margin-inline-start: 0;
  padding-inline: 0;
}

.logo-mark {
  position: relative;
  width: 40px;
  height: 40px;
  border-radius: 12px;
  /* box-shadow: 0 4px 12px rgba(0, 0, 0, 0.25); */
  animation: logoFloat 6s ease-in-out infinite;
  z-index: 1;
}

@keyframes logoFloat {
  0% {
    transform: translateY(0);
  }
  50% {
    transform: translateY(-3px) scale(1.02);
  }
  100% {
    transform: translateY(0);
  }
}

@media (max-width: 640px) {
  .app-header :deep(.v-toolbar__content) {
    padding-block-end: 0;
  }

  .header-actions {
    width: 100%;
    justify-content: flex-start;
    flex-wrap: wrap;
    gap: 6px;
  }

  .quick-upload-label {
    display: none;
  }

  .quick-upload-btn :deep(.v-btn__content) {
    padding-inline: 0;
  }
}

</style>
