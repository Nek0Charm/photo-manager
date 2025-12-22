<script setup lang="ts">
import { computed, ref } from 'vue'
import type { PhotoItem } from '../../types/photos'
import PhotoCard from './PhotoCard.vue'

const props = defineProps<{ photos: PhotoItem[]; loading?: boolean }>()
const emit = defineEmits<{ (e: 'select', payload: { id: number; index: number }): void }>()

const sortOptions = [
  { label: '最新上传', value: 'createdDesc' },
  { label: '最早上传', value: 'createdAsc' },
  { label: '拍摄时间（新→旧）', value: 'takenDesc' },
  { label: '拍摄时间（旧→新）', value: 'takenAsc' },
] as const

const sortValue = ref<(typeof sortOptions)[number]['value']>('createdDesc')

const getTimestamp = (value?: string | null) => (value ? new Date(value).getTime() : 0)

const sortedPhotos = computed(() => {
  const list = [...props.photos]
  switch (sortValue.value) {
    case 'createdAsc':
      return list.sort((a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime())
    case 'takenDesc':
      return list.sort((a, b) => getTimestamp(b.takenAt) - getTimestamp(a.takenAt))
    case 'takenAsc':
      return list.sort((a, b) => getTimestamp(a.takenAt) - getTimestamp(b.takenAt))
    case 'createdDesc':
    default:
      return list.sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
  }
})
</script>

<template>
  <div class="gallery-wrapper">
    <div class="gallery-toolbar">
      <div class="toolbar-label">
        <v-icon icon="mdi-sort" size="18" class="mr-2" />
        排序
      </div>
      <v-select
        v-model="sortValue"
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
        v-for="(photo, index) in sortedPhotos"
        :key="photo.id"
        :photo="photo"
        @select="emit('select', { id: photo.id, index })"
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
  align-items: center;
  justify-content: space-between;
  background: var(--pm-surface-raised);
  border: 1px solid var(--pm-border-subtle);
  border-radius: 12px;
  padding: 8px 16px;
}

.toolbar-label {
  display: flex;
  align-items: center;
  font-weight: 600;
  color: var(--pm-text-muted);
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
