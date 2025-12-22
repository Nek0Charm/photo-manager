import { defineStore } from 'pinia'
import {
  listPhotos,
  uploadPhoto,
  getPhotoDetail,
  editPhoto as editPhotoRequest,
  updatePhotoMetadata as updatePhotoMetadataRequest,
  deletePhoto as deletePhotoRequest,
} from '../services/photos'
import type { PhotoEditPayload, PhotoItem, PhotoListParams, PhotoMetadataPayload, UploadPayload } from '../types/photos'
import { getErrorMessage } from '../utils/errors'
import { API_ASSET_BASE } from '../services/http'

interface DateRange {
  start: string
  end: string
}

const assetBase = API_ASSET_BASE.replace(/\/$/, '')

const toAbsolute = (path: string | null | undefined) => {
  if (!path) return ''
  if (/^https?:\/\//i.test(path)) return path
  if (!assetBase) return path
  const normalizedPath = path.startsWith('/') ? path : `/${path}`
  return `${assetBase}${normalizedPath}`
}

export const usePhotoStore = defineStore('photos', {
  state: () => ({
    items: [] as PhotoItem[],
    total: 0,
    page: 1,
    pageSize: 20,
    query: '',
    selectedTags: [] as string[],
    dateRange: { start: '', end: '' } as DateRange,
    loading: false,
    error: '',
    activePhotoId: null as number | null,
    detailLoading: false,
  }),
  getters: {
    availableTags: (state) =>
      [...new Set(state.items.flatMap((item) => item.tags))].sort((a, b) => a.localeCompare(b)),
    filteredItems: (state) => state.items,
    activePhoto(state) {
      return state.items.find((photo) => photo.id === state.activePhotoId) ?? null
    },
    totalPages(state) {
      return Math.max(1, Math.ceil(state.total / state.pageSize))
    },
  },
  actions: {
    reset() {
      this.items = []
      this.total = 0
      this.page = 1
      this.activePhotoId = null
      this.error = ''
    },
    buildListPayload(): PhotoListParams {
      const payload: PhotoListParams = {
        page: this.page,
        pageSize: this.pageSize,
      }

      const keyword = this.query.trim()
      if (keyword) payload.keyword = keyword

      const tag = this.selectedTags[0]?.trim()
      if (tag) payload.tag = tag

      if (this.dateRange.start) {
        payload.from = new Date(this.dateRange.start).toISOString()
      }

      if (this.dateRange.end) {
        const end = new Date(this.dateRange.end)
        end.setHours(23, 59, 59, 999)
        payload.to = end.toISOString()
      }

      return payload
    },
    async fetchPhotos() {
      this.loading = true
      this.error = ''
      try {
        const response = await listPhotos(this.buildListPayload())
        this.items = response.items.map((item) => ({
          ...item,
          fileUrl: toAbsolute(item.fileUrl),
          thumbnailUrl: toAbsolute(item.thumbnailUrl),
        }))
        this.total = response.total
        if (this.page > this.totalPages) {
          this.page = this.totalPages
        }
      } catch (error) {
        this.error = getErrorMessage(error, '获取图片列表失败')
        throw error
      } finally {
        this.loading = false
      }
    },
    setQuery(value: string) {
      this.query = value
    },
    setTags(tags: string[]) {
      this.selectedTags = tags
    },
    setDateRange(range: DateRange) {
      this.dateRange = range
    },
    setPage(page: number) {
      this.page = page
    },
    clearFilters() {
      this.query = ''
      this.selectedTags = []
      this.dateRange = { start: '', end: '' }
    },
    async upload(payload: UploadPayload) {
      if (!payload.files.length) {
        throw new Error('请选择图片文件')
      }

      for (const file of payload.files) {
        const form = new FormData()
        form.append('File', file)
        if (payload.description) form.append('Description', payload.description)
        if (payload.tags.length) form.append('Tags', payload.tags.join(','))
        if (payload.takenAt) form.append('TakenAt', payload.takenAt)
        if (payload.location) form.append('Location', payload.location)
        await uploadPhoto(form)
      }

      await this.fetchPhotos()
    },
    async selectPhoto(photoId: number) {
      this.activePhotoId = photoId
      await this.fetchDetail(photoId)
    },
    async fetchDetail(photoId: number) {
      this.detailLoading = true
      try {
        const raw = await getPhotoDetail(photoId)
        const detail: PhotoItem = {
          ...raw,
          fileUrl: toAbsolute(raw.fileUrl),
          thumbnailUrl: toAbsolute(raw.thumbnailUrl),
        }
        const index = this.items.findIndex((item) => item.id === detail.id)
        if (index >= 0) {
          this.items[index] = detail
        } else {
          this.items.unshift(detail)
        }
        this.activePhotoId = detail.id
        return detail
      } catch (error) {
        this.error = getErrorMessage(error, '获取图片详情失败')
        throw error
      } finally {
        this.detailLoading = false
      }
    },
    async applyEdit(payload: PhotoEditPayload) {
      if (!payload.file) {
        throw new Error('缺少编辑结果文件')
      }

      const form = new FormData()
      form.append('PhotoId', String(payload.photoId))
      form.append('File', payload.file, payload.fileName ?? `photo-${payload.photoId}.jpg`)

      if (payload.description !== undefined) {
        form.append('Description', payload.description)
      }

      if (payload.tags && payload.tags.length) {
        const normalized = payload.tags.map((tag) => tag.trim()).filter(Boolean)
        if (normalized.length) {
          form.append('Tags', normalized.join(','))
        }
      }

      if (payload.takenAt) {
        form.append('TakenAt', payload.takenAt)
      }

      if (payload.location !== undefined) {
        form.append('Location', payload.location ?? '')
      }

      if (payload.saveAsNew) {
        form.append('SaveAsNew', String(payload.saveAsNew))
      }

      try {
        const response = await editPhotoRequest(form)
        const detail: PhotoItem = {
          ...response,
          fileUrl: toAbsolute(response.fileUrl),
          thumbnailUrl: toAbsolute(response.thumbnailUrl),
        }
        const index = this.items.findIndex((item) => item.id === detail.id)
        if (index >= 0) {
          this.items[index] = detail
        } else {
          this.items.unshift(detail)
        }
        this.activePhotoId = detail.id
        return detail
      } catch (error) {
        this.error = getErrorMessage(error, '应用图片编辑失败')
        throw error
      }
    },
    async updateMetadata(payload: PhotoMetadataPayload) {
      const normalizedTags = payload.tags.map((tag) => tag.trim()).filter(Boolean)
      const description =
        payload.description !== undefined && payload.description !== null ? payload.description.trim() : payload.description

      try {
        const response = await updatePhotoMetadataRequest({
          photoId: payload.photoId,
          description,
          tags: normalizedTags,
        })
        const detail: PhotoItem = {
          ...response,
          fileUrl: toAbsolute(response.fileUrl),
          thumbnailUrl: toAbsolute(response.thumbnailUrl),
        }
        const index = this.items.findIndex((item) => item.id === detail.id)
        if (index >= 0) {
          this.items[index] = detail
        } else {
          this.items.unshift(detail)
        }
        this.activePhotoId = detail.id
        return detail
      } catch (error) {
        this.error = getErrorMessage(error, '更新图片信息失败')
        throw error
      }
    },
    async deletePhoto(photoId: number) {
      try {
        await deletePhotoRequest(photoId)
        this.items = this.items.filter((item) => item.id !== photoId)
        if (this.total > 0) {
          this.total = Math.max(0, this.total - 1)
        }
        if (this.activePhotoId === photoId) {
          const fallback = this.items.length ? this.items[0] : null
          this.activePhotoId = fallback ? fallback.id : null
        }
      } catch (error) {
        this.error = getErrorMessage(error, '删除图片失败')
        throw error
      }
    },
  },
})
