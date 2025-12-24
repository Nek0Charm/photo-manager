<script setup lang="ts">
import { computed } from 'vue'
import type { PhotoItem } from '../../types/photos'

const props = defineProps<{
  photo: PhotoItem
  selectionMode?: boolean
  selected?: boolean
}>()
const emit = defineEmits<{ (e: 'select'): void; (e: 'toggle-select'): void }>()

const isSelectionMode = computed(() => !!props.selectionMode)
const isSelected = computed(() => !!props.selected)

const handleClick = () => {
  if (props.selectionMode) {
    emit('toggle-select')
  } else {
    emit('select')
  }
}

const handleCheckboxClick = (event: MouseEvent) => {
  event.stopPropagation()
  emit('toggle-select')
}
</script>

<template>
  <v-card
    class="photo-card"
    :class="{ 'photo-card--selectable': isSelectionMode, 'photo-card--selected': isSelectionMode && isSelected }"
    rounded="xl"
    elevation="0"
    border
    variant="flat"
    @click="handleClick"
  >
    <v-checkbox-btn
      v-if="isSelectionMode"
      class="photo-card__checkbox"
      color="primary"
      :model-value="isSelected"
      @click="handleCheckboxClick"
    />
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
        <h3 v-if="props.photo.description" class="photo-card__title">
          {{ props.photo.description }}
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
  position: relative;
}

.photo-card--selectable {
  cursor: default;
}

.photo-card--selected {
  border-color: var(--v-primary-base, #1976d2);
  box-shadow: 0 0 0 2px rgba(25, 118, 210, 0.3);
}

.photo-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 10px 30px rgba(15, 23, 42, 0.12);
  border-color: var(--pm-border-strong);
}

.photo-card--selectable:hover {
  transform: none;
  box-shadow: none;
}

.photo-card__checkbox {
  position: absolute;
  top: 12px;
  left: 12px;
  background: rgba(255, 255, 255, 0.9);
  border-radius: 999px;
  padding: 2px;
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.15);
  z-index: 2;
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
