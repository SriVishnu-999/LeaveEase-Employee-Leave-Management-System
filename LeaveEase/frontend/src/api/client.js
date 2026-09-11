const API_URL = import.meta.env.VITE_API_URL || '/api'

export class ApiError extends Error {
  constructor(message, status, payload) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.payload = payload
  }
}

export async function api(path, options = {}) {
  const token = localStorage.getItem('leaveease_token')
  const headers = new Headers(options.headers || {})

  if (!headers.has('Content-Type') && options.body) {
    headers.set('Content-Type', 'application/json')
  }
  if (token) {
    headers.set('Authorization', `Bearer ${token}`)
  }

  const response = await fetch(`${API_URL}${path}`, { ...options, headers })
  const contentType = response.headers.get('content-type') || ''
  const payload = contentType.includes('application/json') ? await response.json() : null

  if (!response.ok) {
    if (response.status === 401) {
      localStorage.removeItem('leaveease_token')
      localStorage.removeItem('leaveease_user')
    }
    throw new ApiError(payload?.message || payload?.title || 'Something went wrong.', response.status, payload)
  }

  return payload
}

export function apiGet(path) {
  return api(path)
}

export function apiPost(path, body) {
  return api(path, { method: 'POST', body: JSON.stringify(body) })
}

export function apiPut(path, body) {
  return api(path, { method: 'PUT', body: body === undefined ? undefined : JSON.stringify(body) })
}
