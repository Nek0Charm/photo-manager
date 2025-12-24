<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import type { PhotoItem } from '../../types/photos'
import { usePhotoStore } from '../../stores/photos'
import { getErrorMessage } from '../../utils/errors'

const props = defineProps<{
  modelValue: boolean
  photo: PhotoItem | null
}>()

const emit = defineEmits<{
  (e: 'update:modelValue', value: boolean): void
}>()

const photoStore = usePhotoStore()

const description = ref('')
const tags = ref<string[]>([])
const newTag = ref('')
const saving = ref(false)
const errorMessage = ref('')
const currentPhoto = computed(() => props.photo)

const dialogTitle = computed(() => {
  if (!currentPhoto.value) {
    return '编辑图片信息'
  }
  return `编辑图片信息 · #${currentPhoto.value.id}`
})

const syncForm = () => {
  description.value = props.photo?.description ?? ''
  tags.value = [...(props.photo?.tags ?? [])]
  newTag.value = ''
  errorMessage.value = ''
}

watch(
  () => props.modelValue,
  (open) => {
    if (open) {
      syncForm()
    } else {
      errorMessage.value = ''
    }
  },
)

watch(
  () => props.photo?.id,
  () => {
    if (props.modelValue) {
      syncForm()
    }
  },
)

const close = () => {
  if (saving.value) {
    return
  }
  emit('update:modelValue', false)
}

const addTag = () => {
  const normalized = newTag.value.trim()
  if (!normalized) {
    return
  }
  if (!tags.value.includes(normalized)) {
    tags.value = [...tags.value, normalized]
  }
  newTag.value = ''
}

const removeTag = (target: string) => {
  tags.value = tags.value.filter((tag) => tag !== target)
}

const handleSave = async () => {
  if (!props.photo) {
    return
  }
  saving.value = true
  errorMessage.value = ''
  try {
    await photoStore.updateMetadata({
      photoId: props.photo.id,
      description: description.value,
      tags: [...tags.value],
    })
    emit('update:modelValue', false)
  } catch (error) {
    errorMessage.value = getErrorMessage(error, '更新信息失败')
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <v-dialog
    :model-value="modelValue"
    max-width="520"
    persistent
    @update:model-value="emit('update:modelValue', $event)"
  >
    <v-card class="metadata-card" rounded="xl">
      <v-card-title class="d-flex align-center justify-space-between">
        <span>{{ dialogTitle }}</span>
        <v-btn icon="mdi-close" variant="text" :disabled="saving" @click="close" />
      </v-card-title>
      <v-card-text>
        <v-alert v-if="errorMessage" type="error" variant="tonal" class="mb-4">
          {{ errorMessage }}
        </v-alert>
        <v-textarea
          v-model="description"
          label="描述"
          variant="outlined"
          rows="4"
          hide-details
          class="mb-4"
          :disabled="!currentPhoto || saving"
        />
        <div class="tag-input-row">
          <v-text-field
            v-model="newTag"
            label="标签"
            variant="outlined"
            hide-details
            :disabled="!currentPhoto || saving"
            placeholder="输入内容并按 Enter 添加"
            @keydown.enter.prevent="addTag"
          />
          <v-btn
            class="ml-3"
            color="primary"
            variant="tonal"
            :disabled="!currentPhoto || saving || !newTag.trim()"
            @click="addTag"
          >
            添加
          </v-btn>
        </div>
        <div v-if="tags.length" class="tag-chip-row mb-2">
          <v-chip
            v-for="tag in tags"
            :key="tag"
            class="mr-2 mb-2"
            color="primary"
            variant="tonal"
            label
            closable
            @click:close="removeTag(tag)"
          >
            {{ tag }}
          </v-chip>
        </div>
      </v-card-text>
      <v-card-actions class="justify-end">
        <v-btn variant="text" :disabled="saving" @click="close">取消</v-btn>
        <v-btn color="primary" :loading="saving" :disabled="!currentPhoto" @click="handleSave">
          保存
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<style scoped>
.metadata-card {
  width: 100%;
}

.field-hint {
  margin: 0;
}

.tag-input-row {
  display: flex;
  align-items: center;
}

.tag-chip-row {
  display: flex;
  flex-wrap: wrap;
}
</style>
