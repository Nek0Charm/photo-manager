<script setup lang="ts">
import { computed } from 'vue'
import type { PhotoItem, PhotoSortOption } from '../../types/photos'
import PhotoCard from './PhotoCard.vue'

const props = defineProps<{
  photos: PhotoItem[]
  loading?: boolean
  sortValue: PhotoSortOption
  selectionMode?: boolean
  selectedIds?: number[]
  bulkDeleting?: boolean
}>()
const emit = defineEmits<{
  (e: 'select', payload: { id: number; index: number }): void
  (e: 'change-sort', value: PhotoSortOption): void
  (e: 'toggle-selection-mode', value: boolean): void
  (e: 'toggle-photo-selection', id: number): void
  (e: 'delete-selected'): void
}>()

const sortOptions = [
  { label: '最新上传', value: 'createdDesc' },
  { label: '最早上传', value: 'createdAsc' },
  { label: '拍摄时间（新→旧）', value: 'takenDesc' },
  { label: '拍摄时间（旧→新）', value: 'takenAsc' },
] as const

const sortValueProxy = computed({
  get: () => props.sortValue,
  set: (value) => emit('change-sort', value),
})

const selectedList = computed(() => props.selectedIds ?? [])
const selectedCount = computed(() => selectedList.value.length)
const hasSelection = computed(() => selectedCount.value > 0)
const isSelected = (id: number) => selectedList.value.includes(id)
</script>

<template>
  <div class="gallery-wrapper">
    <div class="gallery-toolbar">
      <div class="toolbar-left">
        <div class="toolbar-label">
          <v-icon icon="mdi-sort" size="18" class="mr-2" />
          排序
        </div>
        <v-select
          v-model="sortValueProxy"
          :items="sortOptions"
          item-title="label"
          item-value="value"
          variant="outlined"
          density="comfortable"
          hide-details
          class="sort-select"
          :disabled="props.loading || !props.photos.length"
        />
      </div>
      <div class="toolbar-right">
        <template v-if="props.selectionMode">
          <div class="selection-info text-caption text-medium-emphasis">
            已选 <strong>{{ selectedCount }}</strong> 张
          </div>
          <v-btn
            size="small"
            variant="text"
            class="mr-1"
            :disabled="props.loading"
            @click="emit('toggle-selection-mode', false)"
          >
            取消
          </v-btn>
          <v-btn
            size="small"
            color="error"
            variant="tonal"
            :disabled="!hasSelection || props.bulkDeleting"
            :loading="props.bulkDeleting"
            @click="emit('delete-selected')"
          >
            删除已选
          </v-btn>
        </template>
        <template v-else>
          <v-btn
            size="small"
            variant="text"
            :disabled="props.loading || !props.photos.length"
            @click="emit('toggle-selection-mode', true)"
          >
            批量删除
          </v-btn>
        </template>
      </div>
    </div>
    <div v-if="props.loading" class="gallery-grid">
      <v-skeleton-loader
        v-for="index in 6"
        :key="index"
        type="image, list-item-two-line"
        class="pa-4"
        rounded="xl"
      />
    </div>
    <div v-else-if="!props.photos.length" class="empty-state">
      <v-icon icon="mdi-image-off-outline" size="40" class="mb-2" />
      <p class="text-body-2 mb-1">未找到符合条件的图片</p>
      <p class="text-caption text-medium-emphasis">请调整搜索关键词或稍后再试</p>
    </div>
    <div v-else class="gallery-grid">
      <PhotoCard
        v-for="(photo, index) in props.photos"
        :key="photo.id"
        :photo="photo"
        :selection-mode="props.selectionMode"
        :selected="isSelected(photo.id)"
        @select="emit('select', { id: photo.id, index })"
        @toggle-select="emit('toggle-photo-selection', photo.id)"
      />
    </div>
  </div>
</template>

<style scoped>
.gallery-wrapper {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.gallery-toolbar {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  background: var(--pm-surface-raised);
  border: 1px solid var(--pm-border-subtle);
  border-radius: 12px;
  padding: 8px 16px;
}

.toolbar-left,
.toolbar-right {
  display: flex;
  align-items: center;
  gap: 12px;
}

.toolbar-label {
  display: flex;
  align-items: center;
  font-weight: 600;
  color: var(--pm-text-muted);
}

.selection-info strong {
  margin: 0 4px;
  color: var(--v-primary-base, #1976d2);
}

.sort-select {
  max-width: 240px;
}

.gallery-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
  gap: 16px;
}

.empty-state {
  border: 1px dashed var(--pm-border-subtle);
  border-radius: 16px;
  padding: 48px 32px;
  text-align: center;
  color: var(--pm-text-muted);
}
</style>
