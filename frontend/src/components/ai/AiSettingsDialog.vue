<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'
import { storeToRefs } from 'pinia'
import { useAiSettingsStore } from '../../stores/aiSettings'

const props = defineProps<{ modelValue: boolean }>()
const emit = defineEmits<{ (e: 'update:modelValue', value: boolean): void }>()

const aiStore = useAiSettingsStore()
const { data, loading, saving, error } = storeToRefs(aiStore)

const form = reactive({
  model: 'gpt-4o-mini',
  endpoint: '',
  apiKey: '',
  updateApiKey: true,
})

const localError = ref('')

const dialogVisible = computed({
  get: () => props.modelValue,
  set: (value: boolean) => emit('update:modelValue', value),
})

const hasExistingKey = computed(() => data.value?.hasApiKey ?? false)

const syncForm = () => {
  form.model = data.value?.model ?? 'gpt-4o-mini'
  form.endpoint = data.value?.endpoint ?? ''
  form.apiKey = ''
  form.updateApiKey = !hasExistingKey.value
  localError.value = ''
}

watch(
  () => props.modelValue,
  async (open) => {
    if (open) {
      try {
        await aiStore.fetchSettings()
      } catch {
        // errors handled in store
      } finally {
        syncForm()
      }
    } else {
      form.apiKey = ''
      form.updateApiKey = !hasExistingKey.value
      localError.value = ''
    }
  },
)

const validate = () => {
  if (!form.model.trim()) {
    localError.value = '请输入模型名称'
    return false
  }

  if ((!hasExistingKey.value || form.updateApiKey) && !form.apiKey.trim()) {
    localError.value = '请输入可用的 API Key'
    return false
  }

  localError.value = ''
  return true
}

const handleClose = () => {
  dialogVisible.value = false
}

const handleSave = async () => {
  if (!validate()) {
    return
  }

  try {
    await aiStore.saveSettings({
      model: form.model.trim(),
      endpoint: form.endpoint.trim() ? form.endpoint.trim() : null,
      apiKey: (!hasExistingKey.value || form.updateApiKey) ? form.apiKey.trim() : null,
      updateApiKey: !hasExistingKey.value || form.updateApiKey,
    })
    dialogVisible.value = false
  } catch {
    // error already surfaced via store
  }
}

const submitDisabled = computed(() => {
  if (saving.value) return true
  if (!form.model.trim()) return true
  if ((!hasExistingKey.value || form.updateApiKey) && !form.apiKey.trim()) return true
  return false
})
</script>

<template>
  <v-dialog v-model="dialogVisible" max-width="520" persistent>
    <v-card>
      <v-card-title class="text-h6">AI 标签配置</v-card-title>
      <v-card-subtitle>配置当前账户用于智能标签的 API 信息</v-card-subtitle>
      <v-card-text>
        <p class="text-body-2 mb-4">该密钥仅用于当前账户的智能标签生成，不会与其他用户共享。</p>
        <v-alert
          v-if="localError"
          class="mb-3"
          type="error"
          density="comfortable"
          variant="tonal"
        >
          {{ localError }}
        </v-alert>
        <v-alert
          v-else-if="error"
          class="mb-3"
          type="error"
          density="comfortable"
          variant="tonal"
        >
          {{ error }}
        </v-alert>
        <v-progress-linear v-if="loading" indeterminate color="primary" class="mb-4" />

        <v-text-field
          v-model="form.model"
          label="模型"
          placeholder="例如 gpt-4o-mini"
          variant="outlined"
          hide-details="auto"
          :disabled="saving"
          class="mb-4"
        />

        <v-text-field
          v-model="form.endpoint"
          label="Endpoint (可选)"
          placeholder="默认使用官方地址"
          variant="outlined"
          hide-details="auto"
          :disabled="saving"
          class="mb-4"
        />

        <v-alert
          v-if="hasExistingKey"
          class="mb-3"
          type="info"
          density="comfortable"
          variant="tonal"
        >
          已保存的 API Key 将继续使用，除非你选择重新设置。
        </v-alert>

        <v-switch
          v-if="hasExistingKey"
          v-model="form.updateApiKey"
          label="重新设置 API Key"
          color="primary"
          inset
          class="mb-2"
        />

        <v-text-field
          v-if="!hasExistingKey || form.updateApiKey"
          v-model="form.apiKey"
          type="password"
          label="API Key"
          autocomplete="off"
          variant="outlined"
          hide-details="auto"
          :disabled="saving"
        />
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="handleClose">取消</v-btn>
        <v-btn color="primary" :loading="saving" :disabled="submitDisabled" @click="handleSave">
          保存
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>
