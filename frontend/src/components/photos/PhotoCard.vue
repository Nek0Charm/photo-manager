<script setup lang="ts">
import type { PhotoItem } from '../../types/photos'

const props = defineProps<{ photo: PhotoItem }>()
const emit = defineEmits<{ (e: 'select'): void }>()
</script>

<template>
  <v-card
    class="photo-card"
    rounded="xl"
    elevation="0"
    border
    variant="flat"
    @click="emit('select')"
  >
    <v-img
      :src="props.photo.thumbnailUrl || props.photo.fileUrl"
      :alt="props.photo.description ?? 'photo item'"
      height="220"
      cover
      class="photo-card__image"
    >
      <template #placeholder>
        <div class="d-flex align-center justify-center fill-height text-medium-emphasis">
          加载中...
        </div>
      </template>
    </v-img>
    <div class="photo-card__body">
      <div class="d-flex align-center justify-space-between mb-2">
        <h3 class="photo-card__title">
          {{ props.photo.description || '未添加描述' }}
        </h3>
        <span class="photo-card__timestamp text-caption">
          {{ new Date(props.photo.createdAt).toLocaleDateString() }}
        </span>
      </div>
      <p class="photo-card__meta text-body-2 text-medium-emphasis">
        {{ props.photo.location ? props.photo.location : '未知地点' }} · {{ props.photo.width }}×{{ props.photo.height }}
      </p>
      <div class="chip-group">
        <v-chip
          v-for="tag in props.photo.tags"
          :key="tag"
          size="x-small"
          class="mr-1 mb-1"
          color="surface"
          variant="flat"
        >
          {{ tag }}
        </v-chip>
      </div>
    </div>
  </v-card>
</template>

<style scoped>
.photo-card {
  cursor: pointer;
  transition: transform 0.2s ease, box-shadow 0.2s ease, border-color 0.2s ease;
  border-color: var(--pm-border-subtle);
}

.photo-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 10px 30px rgba(15, 23, 42, 0.12);
  border-color: var(--pm-border-strong);
}

.photo-card__body {
  padding: 16px 18px 20px;
}

.photo-card__title {
  font-size: 1rem;
  margin: 0;
  font-weight: 600;
}

.photo-card__meta {
  min-height: 24px;
}

.chip-group {
  display: flex;
  flex-wrap: wrap;
}
</style>
