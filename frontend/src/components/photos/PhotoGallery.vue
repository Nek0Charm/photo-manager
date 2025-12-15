<script setup lang="ts">
import type { PhotoItem } from '../../types/photos'
import PhotoCard from './PhotoCard.vue'

const props = defineProps<{ photos: PhotoItem[]; loading?: boolean }>()
const emit = defineEmits<{ (e: 'select', payload: { id: number; index: number }): void }>()
</script>

<template>
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
    <p class="text-caption text-medium-emphasis">请调整搜索关键词或筛选条件</p>
  </div>
  <div v-else class="gallery-grid">
    <PhotoCard
      v-for="(photo, index) in props.photos"
      :key="photo.id"
      :photo="photo"
      @select="emit('select', { id: photo.id, index })"
    />
  </div>
</template>

<style scoped>
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
