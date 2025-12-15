<script setup lang="ts">
import { ref } from 'vue'
import { usePhotoStore } from '../../stores/photos'
import { getErrorMessage } from '../../utils/errors'

const dialog = ref(false)
const snackbar = ref(false)
const snackbarMessage = ref('')
const snackbarColor = ref<'success' | 'warning' | 'error'>('success')
const description = ref('')
const tags = ref<string[]>([])
const files = ref<File[]>([])
const takenAt = ref('')
const location = ref('')
const submitting = ref(false)

const photoStore = usePhotoStore()

const resetForm = () => {
  description.value = ''
  tags.value = []
  files.value = []
  takenAt.value = ''
  location.value = ''
}

const handleFileChange = (val: File[] | File | null | undefined) => {
  if (!val) {
    files.value = []
    return
  }

  files.value = Array.isArray(val) ? val : [val]
}

const submit = async () => {
  if (!files.value.length) {
    snackbarMessage.value = '请至少选择一张图片进行上传'
    snackbarColor.value = 'warning'
    snackbar.value = true
    return
  }

  submitting.value = true
  try {
    await photoStore.upload({
      description: description.value,
      tags: tags.value,
      files: files.value,
      takenAt: takenAt.value ? new Date(takenAt.value).toISOString() : undefined,
      location: location.value || undefined,
    })
    dialog.value = false
    resetForm()
    snackbarMessage.value = '上传成功，列表已刷新'
    snackbarColor.value = 'success'
  } catch (error) {
    snackbarMessage.value = getErrorMessage(error, '上传失败')
    snackbarColor.value = 'error'
  } finally {
    submitting.value = false
    snackbar.value = true
  }
}

const openDialog = () => {
  dialog.value = true
}

defineExpose({ openDialog })
</script>

<template>
  <v-btn class="upload-fab" color="primary" icon="mdi-plus" size="large" elevation="12" @click="openDialog()" />

  <v-dialog v-model="dialog" max-width="520">
    <v-card rounded="xl">
      <v-card-title class="d-flex align-center justify-space-between">
        <span>上传图片</span>
        <v-btn icon="mdi-close" variant="text" @click="dialog = false" />
      </v-card-title>
      <v-card-text class="pt-0">
        <v-textarea v-model="description" label="描述" variant="outlined" hide-details class="mb-4" rows="3" />
        <v-combobox
          v-model="tags"
          label="标签"
          variant="outlined"
          hide-details
          chips
          multiple
          clearable
          class="mb-4"
        />
        <div class="d-flex ga-3 mb-4">
          <v-text-field v-model="takenAt" type="datetime-local" label="拍摄时间" variant="outlined" hide-details />
          <v-text-field v-model="location" label="拍摄地点" variant="outlined" hide-details />
        </div>
        <v-file-input
          label="选择图片"
          variant="outlined"
          hide-details
          show-size
          accept="image/*"
          prepend-icon="mdi-paperclip"
          multiple
          @update:model-value="handleFileChange"
        />
      </v-card-text>
      <v-card-actions class="justify-end">
        <v-btn variant="text" @click="dialog = false">取消</v-btn>
        <v-btn color="primary" prepend-icon="mdi-upload" :loading="submitting" @click="submit">
          开始上传
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>

  <v-snackbar v-model="snackbar" timeout="2500" :color="snackbarColor">
    {{ snackbarMessage }}
    <template #actions>
      <v-btn variant="text" color="white" @click="snackbar = false">关闭</v-btn>
    </template>
  </v-snackbar>
</template>

<style scoped>
.upload-fab {
  position: fixed;
  bottom: 32px;
  right: 32px;
  z-index: 20;
}
</style>
