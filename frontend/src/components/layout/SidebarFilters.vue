<script setup lang="ts">
import { computed } from 'vue'
import { storeToRefs } from 'pinia'
import { usePhotoStore } from '../../stores/photos'

const emit = defineEmits<{ (e: 'close'): void }>()

const photoStore = usePhotoStore()
const { selectedTags, dateRange } = storeToRefs(photoStore)

const availableTags = computed(() => photoStore.availableTags)

const updateDate = (field: 'start' | 'end', value: string) => {
  photoStore.setDateRange({ ...dateRange.value, [field]: value })
}
</script>

<template>
  <div class="filter-panel">
    <div class="panel-header d-flex align-center justify-space-between mb-4">
      <div>
        <h3 class="text-body-1 font-weight-medium mb-1">筛选器</h3>
        <p class="text-caption text-medium-emphasis">组合标签与时间范围</p>
      </div>
      <v-btn icon="mdi-close" variant="text" density="comfortable" class="d-md-none" @click="emit('close')" />
    </div>

    <section class="mb-6">
      <div class="section-title">标签</div>
      <v-select
        v-model="selectedTags"
        :items="availableTags"
        label="按标签筛选"
        variant="outlined"
        multiple
        chips
        hide-details
        density="comfortable"
        placeholder="选择一个或多个标签"
      >
      </v-select>
    </section>

    <section class="mb-6">
      <div class="section-title">拍摄时间</div>
      <div class="d-flex gap-3">
        <v-text-field
          :model-value="dateRange.start"
          type="date"
          label="开始"
          variant="outlined"
          density="comfortable"
          hide-details
          @update:model-value="updateDate('start', $event)"
        />
        <v-text-field
          :model-value="dateRange.end"
          type="date"
          label="结束"
          variant="outlined"
          density="comfortable"
          hide-details
          @update:model-value="updateDate('end', $event)"
        />
      </div>
    </section>

    <v-btn block variant="tonal" color="primary" prepend-icon="mdi-filter-remove-outline" @click="photoStore.clearFilters()">
      清空筛选条件
    </v-btn>
  </div>
</template>

<style scoped>
.filter-panel {
  padding: 20px;
  height: 100%;
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.section-title {
  font-size: 0.85rem;
  color: var(--pm-text-muted);
  text-transform: uppercase;
  letter-spacing: 0.04em;
  margin-bottom: 8px;
}

.gap-3 {
  gap: 12px;
}
</style>
