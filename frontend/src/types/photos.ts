export interface PhotoItem {
  id: number
  fileUrl: string
  thumbnailUrl: string
  width: number
  height: number
  takenAt?: string | null
  location?: string | null
  description?: string | null
  tags: string[]
  createdAt: string
}

export interface PhotoListResponse {
  total: number
  items: PhotoItem[]
}

export interface PhotoListParams {
  page: number
  pageSize: number
  keyword?: string
  tag?: string
  from?: string
  to?: string
}

export interface UploadPayload {
  description?: string
  tags: string[]
  files: File[]
  takenAt?: string
  location?: string
}

export interface PhotoEditPayload {
  photoId: number
  file: Blob
  fileName?: string
  description?: string
  tags?: string[]
  takenAt?: string
  location?: string | null
  saveAsNew?: boolean
}

export interface PhotoMetadataPayload {
  photoId: number
  description?: string | null
  tags: string[]
}
