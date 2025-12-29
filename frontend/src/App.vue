<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useTheme } from 'vuetify'
import { storeToRefs } from 'pinia'
import AppHeader from './components/layout/AppHeader.vue'
import PhotoGallery from './components/photos/PhotoGallery.vue'
import PhotoViewer from './components/photos/PhotoViewer.vue'
import PhotoEditor from './components/photos/PhotoEditor.vue'
import PhotoMetadataDialog from './components/photos/PhotoMetadataDialog.vue'
import PhotoDeleteDialog from './components/photos/PhotoDeleteDialog.vue'
import UploadFab from './components/upload/UploadFab.vue'
import LoginPanel from './components/auth/LoginPanel.vue'
import AiSettingsDialog from './components/ai/AiSettingsDialog.vue'
import { useUiStore } from './stores/ui'
import { usePhotoStore } from './stores/photos'
import { useUserStore } from './stores/user'
import type { PhotoSortOption } from './types/photos'

const uiStore = useUiStore()
const photoStore = usePhotoStore()
const userStore = useUserStore()
const theme = useTheme()
const { currentTheme } = storeToRefs(uiStore)

const uploadFabRef = ref<InstanceType<typeof UploadFab> | null>(null)
const lightboxOpen = ref(false)
const viewerIndex = ref(0)
const editorOpen = ref(false)
const metadataDialogOpen = ref(false)
const deleteDialogOpen = ref(false)
const aiSettingsDialogOpen = ref(false)

const viewerItems = computed(() => photoStore.items)
const isAuthReady = computed(() => userStore.authChecked)
const selectionMode = computed(() => photoStore.selectionMode)
const selectedPhotoIds = computed(() => photoStore.selectedPhotoIds)
const bulkDeleting = computed(() => photoStore.bulkDeleting)

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
  const items = viewerItems.value
  const targetIndex = items.findIndex((item) => item.id === id)
  viewerIndex.value = targetIndex >= 0 ? targetIndex : index
  lightboxOpen.value = true
  await photoStore.selectPhoto(id).catch(() => undefined)
}

const handleChangeSort = (value: PhotoSortOption) => {
  photoStore.setSortOption(value).catch(() => undefined)
}

const handleToggleSelectionMode = (value: boolean) => {
  photoStore.setSelectionMode(value)
}

const handleTogglePhotoSelection = (photoId: number) => {
  photoStore.togglePhotoSelection(photoId)
}

const handleBulkDeleteSelected = () => {
  photoStore.bulkDeleteSelected().catch(() => undefined)
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

const openUploadDialog = () => {
  uploadFabRef.value?.openDialog()
}

const openAiSettings = () => {
  aiSettingsDialogOpen.value = true
}

const handleEditPhoto = () => {
  if (!photoStore.activePhoto) return
  lightboxOpen.value = false
  editorOpen.value = true
}

const handleEditMetadata = () => {
  if (!photoStore.activePhoto) return
  lightboxOpen.value = false
  metadataDialogOpen.value = true
}

const handleDeletePhoto = () => {
  if (!photoStore.activePhoto) return
  deleteDialogOpen.value = true
}

const handleDeleted = () => {
  deleteDialogOpen.value = false
  lightboxOpen.value = false
}
</script>

<template>
  <v-app class="app-shell">
    <AppHeader @open-upload="openUploadDialog" @open-ai-settings="openAiSettings" />

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
        <PhotoGallery
          :photos="photoStore.items"
          :loading="photoStore.loading"
          :sort-value="photoStore.sortOption"
          :selection-mode="selectionMode"
          :selected-ids="selectedPhotoIds"
          :bulk-deleting="bulkDeleting"
          @select="handleSelectPhoto"
          @change-sort="handleChangeSort"
          @toggle-selection-mode="handleToggleSelectionMode"
          @toggle-photo-selection="handleTogglePhotoSelection"
          @delete-selected="handleBulkDeleteSelected"
        />
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
      @edit="handleEditPhoto"
      @edit-metadata="handleEditMetadata"
      @delete="handleDeletePhoto"
    />

    <PhotoEditor v-model="editorOpen" :photo="photoStore.activePhoto" />
    <PhotoMetadataDialog v-model="metadataDialogOpen" :photo="photoStore.activePhoto" />
    <PhotoDeleteDialog v-model="deleteDialogOpen" :photo="photoStore.activePhoto" @deleted="handleDeleted" />
    <AiSettingsDialog v-model="aiSettingsDialogOpen" />

    <UploadFab v-if="userStore.isAuthenticated" ref="uploadFabRef" />
  </v-app>
</template>

<style scoped>
.app-shell {
  min-height: 100vh;
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
