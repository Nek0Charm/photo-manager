<script setup lang="ts">
import { computed } from 'vue'
import { storeToRefs } from 'pinia'
import { useUiStore } from '../../stores/ui'
import { usePhotoStore } from '../../stores/photos'
import { useUserStore } from '../../stores/user'

const props = defineProps<{ isMobile: boolean }>()
const emit = defineEmits<{ (e: 'toggle-drawer'): void; (e: 'open-upload'): void }>()

const uiStore = useUiStore()
const photoStore = usePhotoStore()
const userStore = useUserStore()
const { query } = storeToRefs(photoStore)
const { user } = storeToRefs(userStore)

const themeIcon = computed(() => (uiStore.currentTheme === 'githubLight' ? 'mdi-weather-night' : 'mdi-white-balance-sunny'))
const themeLabel = computed(() => (uiStore.currentTheme === 'githubLight' ? '切换到暗色模式' : '切换到亮色模式'))

const handleLogout = async () => {
  await userStore.logout()
  photoStore.reset()
}

const runSearch = () => {
  if (userStore.isAuthenticated) {
    photoStore.setPage(1)
    photoStore.fetchPhotos().catch(() => undefined)
  }
}
</script>

<template>
  <v-app-bar flat height="72" class="app-header" color="surface">
    <v-btn
      v-if="props.isMobile"
      icon="mdi-menu"
      variant="text"
      class="mr-2"
      :title="props.isMobile ? '打开筛选面板' : ''"
      @click="emit('toggle-drawer')"
    />
    <v-toolbar-title class="font-weight-bold text-h6">
      <span class="text-primary">Photo</span> Manager
    </v-toolbar-title>
    <v-spacer />
    <v-text-field
      v-model="query"
      density="comfortable"
      variant="outlined"
      hide-details
      prepend-inner-icon="mdi-magnify"
      placeholder="搜索描述或标签"
      class="search-field"
      @keydown.enter.prevent="runSearch"
    />
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
        <v-btn class="ml-2" v-bind="menuProps" variant="tonal" prepend-icon="mdi-account">
          {{ user.username }}
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
  max-width: 420px;
}
</style>
