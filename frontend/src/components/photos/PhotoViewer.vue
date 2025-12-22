<script setup lang="ts">
import { computed } from 'vue'
import type { PhotoItem } from '../../types/photos'

const props = defineProps<{
  items: PhotoItem[]
  index: number
  modelValue: boolean
  loading: boolean
}>()

const emit = defineEmits<{
  (e: 'update:modelValue', value: boolean): void
  (e: 'navigate', direction: 'prev' | 'next'): void
  (e: 'edit', photoId: number | null): void
  (e: 'edit-metadata', photoId: number | null): void
  (e: 'delete', photoId: number | null): void
}>()

const activePhoto = computed(() => props.items[props.index] ?? null)

const close = () => emit('update:modelValue', false)
const goPrev = () => emit('navigate', 'prev')
const goNext = () => emit('navigate', 'next')
const openEditor = () => emit('edit', activePhoto.value?.id ?? null)
const openMetadataEditor = () => emit('edit-metadata', activePhoto.value?.id ?? null)
const requestDelete = () => emit('delete', activePhoto.value?.id ?? null)
</script>

<template>
  <v-dialog :model-value="props.modelValue" fullscreen transition="dialog-bottom-transition" persistent>
    <v-card class="viewer-card" color="black" rounded="0">
      <div class="viewer-toolbar">
        <div class="viewer-meta">
          <p class="viewer-title">{{ activePhoto?.description || '未添加描述' }}</p>
          <p class="viewer-subtitle text-caption">
            {{ activePhoto?.location || '未知地点' }} · {{ activePhoto?.width }}×{{ activePhoto?.height }}
          </p>
        </div>
        <div class="viewer-actions">
          <v-btn
            class="mr-2"
            icon="mdi-tag-text-outline"
            variant="text"
            color="white"
            :disabled="!activePhoto"
            @click="openMetadataEditor"
          />
          <v-btn
            class="mr-2"
            icon="mdi-tune-variant"
            variant="text"
            color="white"
            :disabled="!activePhoto"
            @click="openEditor"
          />
          <v-btn
            class="mr-2"
            icon="mdi-delete-outline"
            variant="text"
            color="error"
            :disabled="!activePhoto"
            @click="requestDelete"
          />
          <v-btn icon="mdi-close" variant="text" color="white" @click="close" />
        </div>
      </div>
      <div class="viewer-body">
        <v-btn
          class="nav-btn"
          icon="mdi-chevron-left"
          variant="text"
          color="white"
          @click.stop="goPrev"
        />
        <div class="viewer-image-wrapper">
          <div v-if="props.loading" class="viewer-loading">
            <v-progress-circular indeterminate size="56" color="white" />
          </div>
          <img
            v-else-if="activePhoto"
            :src="activePhoto.fileUrl"
            :alt="activePhoto.description ?? 'photo'"
            class="viewer-image"
          />
          <p v-if="activePhoto?.tags?.length" class="viewer-tags">
            <v-chip
              v-for="tag in activePhoto.tags"
              :key="tag"
              size="small"
              variant="flat"
              class="mr-2 mb-1"
            >
              {{ tag }}
            </v-chip>
          </p>
        </div>
        <v-btn
          class="nav-btn"
          icon="mdi-chevron-right"
          variant="text"
          color="white"
          @click.stop="goNext"
        />
      </div>
    </v-card>
  </v-dialog>
</template>

<style scoped>
.viewer-card {
  display: flex;
  flex-direction: column;
  height: 100%;
}

.viewer-toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 12px 20px;
  color: white;
}

.viewer-title {
  margin: 0;
  font-size: 1.1rem;
  font-weight: 600;
}

.viewer-subtitle {
  margin: 4px 0 0;
  opacity: 0.75;
}

.viewer-body {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 16px;
  padding: 16px;
}

.viewer-image-wrapper {
  flex: 1;
  max-width: 1200px;
  text-align: center;
}

.viewer-image {
  max-width: 100%;
  max-height: calc(100vh - 140px);
  object-fit: contain;
  border-radius: 12px;
  box-shadow: 0 10px 30px rgba(0, 0, 0, 0.4);
}

.viewer-tags {
  margin-top: 12px;
  color: white;
}

.nav-btn {
  min-width: 48px;
  height: 48px;
}

.viewer-loading {
  height: calc(100vh - 140px);
  display: flex;
  align-items: center;
  justify-content: center;
}
</style>
