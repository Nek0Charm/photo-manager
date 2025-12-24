<script setup lang="ts">
import { computed, onBeforeUnmount, watch } from 'vue'
import { storeToRefs } from 'pinia'
import { useDisplay } from 'vuetify'
import logoMark from '../../assets/logo.svg'
import { useUiStore } from '../../stores/ui'
import { usePhotoStore } from '../../stores/photos'
import { useUserStore } from '../../stores/user'

const emit = defineEmits<{ (e: 'open-upload'): void }>()

const SEARCH_DEBOUNCE_MS = 500
let searchDebounceTimer: ReturnType<typeof setTimeout> | null = null

const uiStore = useUiStore()
const photoStore = usePhotoStore()
const userStore = useUserStore()
const { query } = storeToRefs(photoStore)
const { user } = storeToRefs(userStore)
const { smAndDown } = useDisplay()

const displayUsername = computed(() => {
  if (!user.value?.username) return ''
  const username = user.value.username.trim()
  return smAndDown.value ? username.charAt(0).toUpperCase() : username
})

const themeIcon = computed(() => (uiStore.currentTheme === 'githubLight' ? 'mdi-weather-night' : 'mdi-white-balance-sunny'))
const themeLabel = computed(() => (uiStore.currentTheme === 'githubLight' ? '切换到暗色模式' : '切换到亮色模式'))

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
  <v-app-bar flat height="72" class="app-header" color="surface">
    <v-toolbar-title class="logo-title font-weight-bold text-h6 d-flex align-center">
      <img :src="logoMark" alt="Photo Manager logo" class="logo-mark" />
    </v-toolbar-title>
    <v-spacer />
    <div class="search-field flex-grow-1">
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
    <v-btn
      class="ml-4"
      icon
      :title="themeLabel"
      variant="text"
      @click="uiStore.toggleTheme"
    >
      <v-icon :icon="themeIcon" />
    </v-btn>
    <v-menu v-if="user" transition="fade-transition" offset-y>
      <template #activator="{ props: menuProps }">
        <v-btn class="ml-2" v-bind="menuProps" variant="tonal" prepend-icon="mdi-account" :title="user?.username || ''">
          {{ displayUsername }}
        </v-btn>
      </template>
      <v-list density="compact">
        <v-list-item title="退出登录" prepend-icon="mdi-logout" @click="handleLogout" />
      </v-list>
    </v-menu>
    <v-btn
      class="ml-2"
      color="primary"
      prepend-icon="mdi-upload"
      :disabled="!userStore.isAuthenticated"
      @click="emit('open-upload')"
    >
      快速上传
    </v-btn>
  </v-app-bar>
</template>

<style scoped>
.app-header {
  border-bottom: 1px solid var(--pm-border-subtle);
  padding-inline: 16px;
}

.search-field {
  display: flex;
  flex-direction: column;
  gap: 4px;
  max-width: 420px;
  flex: 1 1 240px;
  min-width: 160px;
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

@media (max-width: 640px) {
  .search-field {
    flex: 1 1 100%;
    min-width: 0;
  }
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

</style>
