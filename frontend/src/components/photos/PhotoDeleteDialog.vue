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
  (e: 'deleted'): void
}>()

const photoStore = usePhotoStore()
const deleting = ref(false)
const errorMessage = ref('')
const currentPhoto = computed(() => props.photo)

watch(
  () => props.modelValue,
  (open) => {
    if (!open) {
      errorMessage.value = ''
    }
  },
)

const close = () => {
  if (deleting.value) {
    return
  }
  emit('update:modelValue', false)
}

const confirmDelete = async () => {
  if (!currentPhoto.value) {
    return
  }
  deleting.value = true
  errorMessage.value = ''
  try {
    await photoStore.deletePhoto(currentPhoto.value.id)
    emit('deleted')
    emit('update:modelValue', false)
  } catch (error) {
    errorMessage.value = getErrorMessage(error, '删除失败')
  } finally {
    deleting.value = false
  }
}
</script>

<template>
  <v-dialog
    :model-value="modelValue"
    max-width="420"
    persistent
    @update:model-value="emit('update:modelValue', $event)"
  >
    <v-card class="delete-card" rounded="xl">
      <v-card-title class="d-flex align-center">
        <v-icon icon="mdi-alert" color="error" class="mr-3" />
        <span>删除照片</span>
      </v-card-title>
      <v-card-text>
        <v-alert v-if="errorMessage" type="error" variant="tonal" class="mb-4">
          {{ errorMessage }}
        </v-alert>
        <p class="mb-3">
          确认删除该照片吗？操作不可撤销。
        </p>
        <p class="text-medium-emphasis text-body-2">
          {{ currentPhoto?.description || '未命名照片' }} · ID #{{ currentPhoto?.id }}
        </p>
      </v-card-text>
      <v-card-actions class="justify-end">
        <v-btn variant="text" :disabled="deleting" @click="close">取消</v-btn>
        <v-btn color="error" :loading="deleting" :disabled="!currentPhoto" @click="confirmDelete">
          删除
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<style scoped>
.delete-card {
  width: 100%;
}
</style>
