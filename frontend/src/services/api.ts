import axios, { type AxiosError } from 'axios'

// Error types for better handling
export interface ApiError {
  type: 'network' | 'validation' | 'auth' | 'server' | 'unknown'
  message: string
  errors?: string[]
  status?: number
}

// Extract error message from API response or network error
export function extractApiError(error: unknown): ApiError {
  // Network errors (no response)
  if (axios.isAxiosError(error)) {
    const axiosError = error as AxiosError<{ errors?: string[]; message?: string }>

    // No response - network/timeout error
    if (!axiosError.response) {
      if (axiosError.code === 'ECONNABORTED' || axiosError.message.includes('timeout')) {
        return {
          type: 'network',
          message: 'Request timed out. Please check your connection and try again.',
        }
      }
      return {
        type: 'network',
        message: 'Unable to connect to the server. Please check your internet connection.',
      }
    }

    const status = axiosError.response.status
    const data = axiosError.response.data

    // Authentication errors
    if (status === 401) {
      return {
        type: 'auth',
        message: 'Session expired. Please log in again.',
        status,
      }
    }

    // Forbidden
    if (status === 403) {
      return {
        type: 'auth',
        message: 'You do not have permission to perform this action.',
        status,
      }
    }

    // Validation errors
    if (status === 400 || status === 422) {
      const errors = data?.errors || []
      return {
        type: 'validation',
        message: errors[0] || 'Invalid request. Please check your input.',
        errors,
        status,
      }
    }

    // Not found
    if (status === 404) {
      return {
        type: 'server',
        message: data?.errors?.[0] || 'The requested resource was not found.',
        status,
      }
    }

    // Rate limiting
    if (status === 429) {
      return {
        type: 'server',
        message: 'Too many requests. Please wait a moment and try again.',
        status,
      }
    }

    // Server errors
    if (status >= 500) {
      return {
        type: 'server',
        message: 'Something went wrong on our end. Please try again later.',
        status,
      }
    }

    // Other errors with response data
    return {
      type: 'unknown',
      message: data?.errors?.[0] || data?.message || 'An unexpected error occurred.',
      errors: data?.errors,
      status,
    }
  }

  // Non-axios errors
  return {
    type: 'unknown',
    message: error instanceof Error ? error.message : 'An unexpected error occurred.',
  }
}

const api = axios.create({
  baseURL: '/api',
  timeout: 30000, // 30 second timeout
  headers: {
    'Content-Type': 'application/json',
  },
})

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem('token')
      // Only redirect if not already on login page
      if (!window.location.pathname.includes('/login')) {
        window.location.href = '/login'
      }
    }
    return Promise.reject(error)
  }
)

export default api
