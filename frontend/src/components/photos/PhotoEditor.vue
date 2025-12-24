<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, reactive, ref, watch } from 'vue'
import Cropper from 'cropperjs'
import 'cropperjs/dist/cropper.css'
import type { PhotoItem } from '../../types/photos'
import { usePhotoStore } from '../../stores/photos'
import { getErrorMessage } from '../../utils/errors'

type AspectKey = 'free' | '1-1' | '4-3' | '16-9'
type AdjustmentKey = 'brightness' | 'contrast' | 'saturation' | 'grayscale'

const aspectOptions: Array<{ label: string; value: AspectKey }> = [
  { label: '自由', value: 'free' },
  { label: '1:1', value: '1-1' },
  { label: '4:3', value: '4-3' },
  { label: '16:9', value: '16-9' },
]

const sliderOptions: Array<{ key: AdjustmentKey; label: string; min: number; max: number }> = [
  { key: 'brightness', label: '亮度', min: 50, max: 150 },
  { key: 'contrast', label: '对比度', min: 50, max: 150 },
  { key: 'saturation', label: '饱和度', min: 50, max: 150 },
  { key: 'grayscale', label: '灰度', min: 0, max: 100 },
]

const ratioMap: Record<AspectKey, number | undefined> = {
  free: undefined,
  '1-1': 1,
  '4-3': 4 / 3,
  '16-9': 16 / 9,
}

const props = defineProps<{
  modelValue: boolean
  photo: PhotoItem | null
}>()

const emit = defineEmits<{
  (e: 'update:modelValue', value: boolean): void
  (e: 'saved'): void
}>()

const photoStore = usePhotoStore()

const saving = ref(false)
const errorMessage = ref('')
const imageRef = ref<HTMLImageElement | null>(null)
const cropper = ref<Cropper | null>(null)
const aspectValue = ref<AspectKey>('free')

const adjustments = reactive<Record<AdjustmentKey, number>>({
  brightness: 100,
  contrast: 100,
  saturation: 100,
  grayscale: 0,
})

const filterString = computed(
  () =>
    `brightness(${adjustments.brightness}%) contrast(${adjustments.contrast}%) saturate(${adjustments.saturation}%) grayscale(${adjustments.grayscale}%)`,
)

const filterStyle = computed(() => ({
  '--editor-filter': filterString.value,
}))

const imageKey = computed(() => props.photo?.fileUrl ?? '')

const close = () => {
  if (saving.value) {
    return
  }
  emit('update:modelValue', false)
}

const destroyCropper = () => {
  if (cropper.value) {
    cropper.value.destroy()
    cropper.value = null
  }
}

const initCropper = () => {
  if (!props.photo || !imageRef.value) {
    return
  }

  destroyCropper()
  cropper.value = new Cropper(imageRef.value, {
    viewMode: 1,
    background: false,
    autoCropArea: 1,
    responsive: true,
    dragMode: 'move',
    movable: true,
    scalable: true,
    zoomable: true,
    checkOrientation: false,
  })
  applyAspectRatio(aspectValue.value)
}

const resetAdjustments = () => {
  adjustments.brightness = 100
  adjustments.contrast = 100
  adjustments.saturation = 100
  adjustments.grayscale = 0
  aspectValue.value = 'free'
  if (cropper.value) {
    cropper.value.reset()
    cropper.value.setAspectRatio(NaN)
  }
}

const applyAspectRatio = (value: AspectKey) => {
  const ratio = ratioMap[value]
  cropper.value?.setAspectRatio(ratio ?? NaN)
}

const rotate = (delta: number) => {
  cropper.value?.rotate(delta)
}

const handleImageLoaded = () => {
  nextTick(() => initCropper())
}

const setAdjustment = (key: AdjustmentKey, value: number) => {
  adjustments[key] = value
}

const exportEditedBlob = async () => {
  if (!cropper.value) {
    throw new Error('编辑器尚未就绪')
  }

  const baseCanvas = cropper.value.getCroppedCanvas({
    imageSmoothingEnabled: true,
    imageSmoothingQuality: 'high',
    fillColor: '#000',
  })

  if (!baseCanvas) {
    throw new Error('无法生成裁剪结果')
  }

  const filteredCanvas = document.createElement('canvas')
  filteredCanvas.width = baseCanvas.width
  filteredCanvas.height = baseCanvas.height
  const ctx = filteredCanvas.getContext('2d')

  if (!ctx) {
    throw new Error('浏览器不支持 Canvas 渲染')
  }

  ctx.filter = filterString.value
  ctx.drawImage(baseCanvas, 0, 0)

  return await new Promise<Blob>((resolve, reject) => {
    filteredCanvas.toBlob((blob) => {
      if (blob) {
        resolve(blob)
      } else {
        reject(new Error('生成图片失败'))
      }
    }, 'image/jpeg', 0.92)
  })
}

const handleSave = async (saveAsNew = false) => {
  if (!props.photo) {
    return
  }

  saving.value = true
  errorMessage.value = ''
  try {
    const blob = await exportEditedBlob()
    await photoStore.applyEdit({
      photoId: props.photo.id,
      file: blob,
      fileName: `edited-${props.photo.id}.jpg`,
      saveAsNew,
    })
    emit('saved')
    emit('update:modelValue', false)
  } catch (error) {
    errorMessage.value = getErrorMessage(error, saveAsNew ? '另存为失败' : '保存编辑结果失败')
  } finally {
    saving.value = false
  }
}

watch(
  () => props.modelValue,
  (open) => {
    if (open) {
      errorMessage.value = ''
      resetAdjustments()
      nextTick(() => {
        if (imageRef.value?.complete) {
          initCropper()
        }
      })
    } else {
      destroyCropper()
    }
  },
)

watch(
  () => props.photo?.fileUrl,
  () => {
    if (!props.modelValue) {
      return
    }
    resetAdjustments()
    nextTick(() => {
      if (imageRef.value?.complete) {
        initCropper()
      }
    })
  },
)

watch(aspectValue, (value) => applyAspectRatio(value))

onBeforeUnmount(() => destroyCropper())
</script>

<template>
  <v-dialog
    :model-value="modelValue"
    fullscreen
    scrollable
    transition="dialog-bottom-transition"
    @update:model-value="emit('update:modelValue', $event)"
  >
    <v-card class="editor-card" color="surface">
      <div class="editor-toolbar">
        <div>
          <p class="editor-title">图片编辑</p>
          <p class="editor-subtitle text-caption mb-0">
            {{ props.photo?.description || '未命名照片' }} · {{ props.photo?.width }}×{{ props.photo?.height }}
          </p>
        </div>
        <div class="editor-toolbar-actions">
          <v-btn
            icon="mdi-rotate-left"
            variant="text"
            color="primary"
            class="mr-1"
            :disabled="!props.photo || saving"
            @click="rotate(-90)"
          />
          <v-btn
            icon="mdi-rotate-right"
            variant="text"
            color="primary"
            class="mr-4"
            :disabled="!props.photo || saving"
            @click="rotate(90)"
          />
          <v-btn class="mr-2" variant="text" :disabled="!props.photo || saving" @click="resetAdjustments">
            重置
          </v-btn>
          <v-btn
            class="mr-2"
            variant="tonal"
            color="primary"
            :loading="saving"
            :disabled="!props.photo"
            @click="handleSave(true)"
          >
            另存为
          </v-btn>
          <v-btn color="primary" :loading="saving" :disabled="!props.photo" @click="() => handleSave(false)">保存</v-btn>
          <v-btn icon="mdi-close" variant="text" class="ml-2" :disabled="saving" @click="close" />
        </div>
      </div>

      <v-alert v-if="errorMessage" type="error" class="mb-0" variant="tonal">
        {{ errorMessage }}
      </v-alert>

      <div class="editor-body">
        <div class="editor-preview" :class="{ 'editor-preview--empty': !props.photo }" :style="filterStyle">
          <img
            v-if="props.photo"
            :key="imageKey"
            ref="imageRef"
            :src="props.photo.fileUrl"
            alt="preview"
            class="editor-image"
            @load="handleImageLoaded"
          />
          <div v-else class="editor-empty">
            <v-icon icon="mdi-image-off" size="48" class="mb-2" />
            <p>请选择要编辑的图片</p>
          </div>
        </div>
        <div class="editor-controls">
          <section class="control-section">
            <p class="section-title">裁剪比例</p>
            <v-btn-toggle
              v-model="aspectValue"
              class="aspect-toggle"
              color="primary"
              divided
              rounded="lg"
            >
              <v-btn
                v-for="option in aspectOptions"
                :key="option.value"
                :value="option.value"
                variant="tonal"
                :disabled="!props.photo || saving"
              >
                {{ option.label }}
              </v-btn>
            </v-btn-toggle>
          </section>

          <section class="control-section">
            <p class="section-title">滤镜调整</p>
            <div class="slider-stack">
              <div v-for="item in sliderOptions" :key="item.key" class="slider-row">
                <div class="slider-label">
                  <span>{{ item.label }}</span>
                  <span class="slider-value">{{ adjustments[item.key] }}%</span>
                </div>
                <v-slider
                  :model-value="adjustments[item.key]"
                  :min="item.min"
                  :max="item.max"
                  :disabled="!props.photo || saving"
                  :step="item.key === 'grayscale' ? 1 : 5"
                  color="primary"
                  track-size="4"
                  @update:model-value="(value) => setAdjustment(item.key, Number(value))"
                />
              </div>
            </div>
          </section>
        </div>
      </div>
    </v-card>
  </v-dialog>
</template>

<style scoped>
.editor-card {
  display: flex;
  flex-direction: column;
  height: 100vh;
}

.editor-toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px 24px;
  border-bottom: 1px solid var(--pm-border-subtle);
}

.editor-title {
  margin: 0;
  font-size: 1.2rem;
  font-weight: 600;
}

.editor-subtitle {
  margin: 4px 0 0;
  color: var(--pm-text-muted);
}

.editor-toolbar-actions {
  display: flex;
  align-items: center;
}

.editor-body {
  flex: 1;
  display: grid;
  grid-template-columns: minmax(0, 3fr) minmax(320px, 2fr);
  gap: 24px;
  padding: 24px;
}

.editor-preview {
  min-height: 0;
  border-radius: 16px;
  background: #000;
  display: flex;
  align-items: center;
  justify-content: center;
  position: relative;
  overflow: hidden;
}

.editor-preview--empty {
  border: 1px dashed var(--pm-border-strong);
  background: transparent;
}

.editor-image {
  max-width: 100%;
  max-height: 100%;
  display: block;
}

.editor-empty {
  text-align: center;
  color: var(--pm-text-muted);
}

.editor-controls {
  overflow-y: auto;
  padding-right: 8px;
}

.control-section + .control-section {
  margin-top: 24px;
}

.section-title {
  font-size: 0.95rem;
  font-weight: 600;
  margin-bottom: 8px;
}

.slider-stack {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.slider-row {
  background: var(--pm-surface-raised);
  border-radius: 12px;
  padding: 12px 16px;
  border: 1px solid var(--pm-border-subtle);
}

.slider-label {
  display: flex;
  align-items: center;
  justify-content: space-between;
  font-size: 0.85rem;
  margin-bottom: 4px;
}

.slider-value {
  font-variant-numeric: tabular-nums;
}

.aspect-toggle {
  width: 100%;
}

.editor-preview :deep(.cropper-container) {
  width: 100% !important;
  height: 100% !important;
}

.editor-preview :deep(.cropper-canvas img),
.editor-preview :deep(.cropper-view-box img) {
  filter: var(--editor-filter);
}

@media (max-width: 960px) {
  .editor-body {
    grid-template-columns: 1fr;
    grid-template-rows: minmax(0, 1fr) auto;
  }

  .editor-preview {
    min-height: 55vh;
  }

  .editor-controls {
    padding-right: 0;
    max-height: 45vh;
  }
}
</style>
