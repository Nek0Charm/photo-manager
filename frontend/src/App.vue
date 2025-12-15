<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useDisplay, useTheme } from 'vuetify'
import { storeToRefs } from 'pinia'
import AppHeader from './components/layout/AppHeader.vue'
import SidebarFilters from './components/layout/SidebarFilters.vue'
import PhotoGallery from './components/photos/PhotoGallery.vue'
import PhotoViewer from './components/photos/PhotoViewer.vue'
import UploadFab from './components/upload/UploadFab.vue'
import LoginPanel from './components/auth/LoginPanel.vue'
import { useUiStore } from './stores/ui'
import { usePhotoStore } from './stores/photos'
import { useUserStore } from './stores/user'

const uiStore = useUiStore()
const photoStore = usePhotoStore()
const userStore = useUserStore()
const theme = useTheme()
const { mobile } = useDisplay()
const { currentTheme } = storeToRefs(uiStore)

const drawer = ref(true)
const uploadFabRef = ref<InstanceType<typeof UploadFab> | null>(null)
const lightboxOpen = ref(false)
const viewerIndex = ref(0)

const viewerItems = computed(() => photoStore.filteredItems)
const isAuthReady = computed(() => userStore.authChecked)

watch(
  () => mobile.value,
  (isMobile) => {
    drawer.value = !isMobile
  },
  { immediate: true },
)

watch(
  () => currentTheme.value,
  (value) => {
    theme.global.name.value = value
  },
)

const triggerFilterFetch = () => {
  if (!userStore.isAuthenticated) return
  photoStore.setPage(1)
  photoStore.fetchPhotos().catch(() => undefined)
}


watch(
  () => photoStore.selectedTags,
  () => {
    triggerFilterFetch()
  },
  { deep: true },
)

watch(
  () => [photoStore.dateRange.start, photoStore.dateRange.end],
  () => {
    triggerFilterFetch()
  },
)

watch(
  () => photoStore.page,
  (newValue, oldValue) => {
    if (!userStore.isAuthenticated) return
    if (newValue === oldValue) return
    photoStore.fetchPhotos().catch(() => undefined)
  },
)

onMounted(async () => {
  uiStore.bootstrap()
  theme.global.name.value = uiStore.currentTheme
  await userStore.fetchCurrentUser()
  if (userStore.isAuthenticated) {
    await photoStore.fetchPhotos()
  }
})

onBeforeUnmount(() => {
  uiStore.teardown()
})

const handleSelectPhoto = async ({ id, index }: { id: number; index: number }) => {
  viewerIndex.value = index
  lightboxOpen.value = true
  await photoStore.selectPhoto(id).catch(() => undefined)
}

const handleNavigate = (direction: 'prev' | 'next') => {
  const items = viewerItems.value
  if (!items.length) return
  const delta = direction === 'next' ? 1 : -1
  viewerIndex.value = (viewerIndex.value + delta + items.length) % items.length
  const target = items[viewerIndex.value]
  if (target) {
    photoStore.selectPhoto(target.id).catch(() => undefined)
  }
}

const toggleDrawer = () => {
  drawer.value = !drawer.value
}

const openUploadDialog = () => {
  uploadFabRef.value?.openDialog()
}
</script>

<template>
  <v-app class="app-shell">
    <AppHeader :is-mobile="mobile" @toggle-drawer="toggleDrawer" @open-upload="openUploadDialog" />

    <v-navigation-drawer
      v-if="userStore.isAuthenticated"
      v-model="drawer"
      class="app-drawer"
      :width="320"
      :temporary="mobile"
      floating
      touchless
    >
      <SidebarFilters @close="drawer = false" />
    </v-navigation-drawer>

    <v-main class="app-main">
      <div v-if="!isAuthReady" class="auth-loading">
        <v-progress-circular indeterminate color="primary" size="48" />
      </div>
      <LoginPanel v-else-if="!userStore.isAuthenticated" />
      <div v-else class="page-shell">
        <div class="page-heading d-flex align-center justify-space-between mb-6">
          <div>
            <h1 class="page-title">照片库</h1>
          </div>
          <div class="page-meta text-medium-emphasis">
            共 {{ photoStore.total }} 张图片
          </div>
        </div>
        <v-alert
          v-if="photoStore.error"
          type="error"
          variant="tonal"
          class="mb-4"
          border="start"
          border-color="error"
        >
          {{ photoStore.error }}
        </v-alert>
        <PhotoGallery :photos="photoStore.filteredItems" :loading="photoStore.loading" @select="handleSelectPhoto" />
        <div v-if="photoStore.totalPages > 1" class="pagination-shell">
          <v-pagination
            v-model="photoStore.page"
            :length="photoStore.totalPages"
            rounded="circle"
            total-visible="7"
          />
        </div>
      </div>
    </v-main>

    <PhotoViewer
      v-model="lightboxOpen"
      :items="viewerItems"
      :index="viewerIndex"
      :loading="photoStore.detailLoading"
      @navigate="handleNavigate"
    />

    <UploadFab v-if="userStore.isAuthenticated" ref="uploadFabRef" />
  </v-app>
</template>

<style scoped>
.app-shell {
  min-height: 100vh;
}

.app-drawer {
  border-right: 1px solid var(--pm-border-subtle);
}

.app-main {
  background: var(--pm-surface-main);
}

.page-shell {
  padding: 32px 32px 60px;
  max-width: 1400px;
  margin: 0 auto;
}

.page-title {
  font-size: 1.4rem;
  margin: 0;
}

.page-subtitle {
  margin: 4px 0 0;
}

.pagination-shell {
  display: flex;
  justify-content: center;
  margin-top: 24px;
}

.auth-loading {
  min-height: calc(100vh - 120px);
  display: flex;
  align-items: center;
  justify-content: center;
}

@media (max-width: 960px) {
  .page-shell {
    padding: 24px 16px 80px;
  }
}
</style>
