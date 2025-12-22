import http from './http'
import type { PhotoItem, PhotoListParams, PhotoListResponse, PhotoMetadataPayload } from '../types/photos'

export function listPhotos(params: PhotoListParams) {
  return http.post<PhotoListResponse>('/photos/list', params).then((res) => res.data)
}

export function uploadPhoto(formData: FormData) {
  return http.post('/photos/upload', formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  })
}

export function editPhoto(formData: FormData) {
  return http.post<PhotoItem>('/photos/edit', formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  }).then((res) => res.data)
}

export function updatePhotoMetadata(payload: PhotoMetadataPayload) {
  return http.post<PhotoItem>('/photos/update-metadata', payload).then((res) => res.data)
}

export function getPhotoDetail(id: number) {
  return http.post<PhotoItem>('/photos/detail', { id }).then((res) => res.data)
}
