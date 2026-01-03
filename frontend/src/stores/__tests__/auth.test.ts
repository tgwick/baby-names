import { describe, it, expect, beforeEach, vi } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useAuthStore } from '../auth'

// Mock the api module
vi.mock('@/services/api', () => ({
  default: {
    post: vi.fn(),
    get: vi.fn(),
  },
}))

// Mock localStorage
const localStorageMock = {
  getItem: vi.fn(),
  setItem: vi.fn(),
  removeItem: vi.fn(),
}
Object.defineProperty(window, 'localStorage', { value: localStorageMock })

import api from '@/services/api'

describe('Auth Store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
    localStorageMock.getItem.mockReturnValue(null)
  })

  describe('initial state', () => {
    it('should have null user initially', () => {
      const store = useAuthStore()
      expect(store.user).toBeNull()
    })

    it('should have null token initially when localStorage is empty', () => {
      const store = useAuthStore()
      expect(store.token).toBeNull()
    })

    it('should not be authenticated initially', () => {
      const store = useAuthStore()
      expect(store.isAuthenticated).toBe(false)
    })
  })

  describe('login', () => {
    it('should set user and token on successful login', async () => {
      const mockUser = { id: '123', email: 'test@example.com', displayName: 'Test User' }
      const mockToken = 'jwt-token-123'

      vi.mocked(api.post).mockResolvedValueOnce({
        data: { data: { token: mockToken, user: mockUser } },
      })

      const store = useAuthStore()
      await store.login('test@example.com', 'password123')

      expect(store.user).toEqual(mockUser)
      expect(store.token).toBe(mockToken)
      expect(store.isAuthenticated).toBe(true)
      expect(localStorageMock.setItem).toHaveBeenCalledWith('token', mockToken)
    })

    it('should call api with correct credentials', async () => {
      vi.mocked(api.post).mockResolvedValueOnce({
        data: { data: { token: 'token', user: { id: '1', email: 'test@example.com' } } },
      })

      const store = useAuthStore()
      await store.login('user@test.com', 'mypassword')

      expect(api.post).toHaveBeenCalledWith('/auth/login', {
        email: 'user@test.com',
        password: 'mypassword',
      })
    })

    it('should throw error on failed login', async () => {
      vi.mocked(api.post).mockRejectedValueOnce(new Error('Invalid credentials'))

      const store = useAuthStore()
      await expect(store.login('bad@email.com', 'wrong')).rejects.toThrow('Invalid credentials')
      expect(store.user).toBeNull()
      expect(store.token).toBeNull()
    })
  })

  describe('register', () => {
    it('should set user and token on successful registration', async () => {
      const mockUser = { id: '456', email: 'new@example.com', displayName: 'New User' }
      const mockToken = 'new-jwt-token'

      vi.mocked(api.post).mockResolvedValueOnce({
        data: { data: { token: mockToken, user: mockUser } },
      })

      const store = useAuthStore()
      await store.register('new@example.com', 'password123', 'New User')

      expect(store.user).toEqual(mockUser)
      expect(store.token).toBe(mockToken)
      expect(store.isAuthenticated).toBe(true)
    })

    it('should call api with correct registration data', async () => {
      vi.mocked(api.post).mockResolvedValueOnce({
        data: { data: { token: 'token', user: { id: '1', email: 'test@example.com' } } },
      })

      const store = useAuthStore()
      await store.register('new@example.com', 'password', 'Display Name')

      expect(api.post).toHaveBeenCalledWith('/auth/register', {
        email: 'new@example.com',
        password: 'password',
        displayName: 'Display Name',
      })
    })

    it('should work without displayName', async () => {
      vi.mocked(api.post).mockResolvedValueOnce({
        data: { data: { token: 'token', user: { id: '1', email: 'test@example.com' } } },
      })

      const store = useAuthStore()
      await store.register('new@example.com', 'password')

      expect(api.post).toHaveBeenCalledWith('/auth/register', {
        email: 'new@example.com',
        password: 'password',
        displayName: undefined,
      })
    })
  })

  describe('fetchUser', () => {
    it('should fetch and set user when token exists', async () => {
      const mockUser = { id: '123', email: 'test@example.com', displayName: 'Test' }

      // First login to set token
      vi.mocked(api.post).mockResolvedValueOnce({
        data: { data: { token: 'token', user: mockUser } },
      })

      const store = useAuthStore()
      await store.login('test@example.com', 'password')

      // Now mock fetchUser response
      vi.mocked(api.get).mockResolvedValueOnce({
        data: { data: mockUser },
      })

      await store.fetchUser()

      expect(api.get).toHaveBeenCalledWith('/auth/me')
      expect(store.user).toEqual(mockUser)
    })

    it('should not fetch when no token', async () => {
      const store = useAuthStore()
      await store.fetchUser()

      expect(api.get).not.toHaveBeenCalled()
    })

    it('should logout on fetch error', async () => {
      vi.mocked(api.post).mockResolvedValueOnce({
        data: { data: { token: 'token', user: { id: '1', email: 'test@example.com' } } },
      })

      const store = useAuthStore()
      await store.login('test@example.com', 'password')

      vi.mocked(api.get).mockRejectedValueOnce(new Error('Unauthorized'))

      await store.fetchUser()

      expect(store.user).toBeNull()
      expect(store.token).toBeNull()
      expect(localStorageMock.removeItem).toHaveBeenCalledWith('token')
    })
  })

  describe('logout', () => {
    it('should clear user and token on logout', async () => {
      vi.mocked(api.post).mockResolvedValueOnce({
        data: { data: { token: 'token', user: { id: '1', email: 'test@example.com' } } },
      })

      const store = useAuthStore()
      await store.login('test@example.com', 'password')

      expect(store.isAuthenticated).toBe(true)

      store.logout()

      expect(store.user).toBeNull()
      expect(store.token).toBeNull()
      expect(store.isAuthenticated).toBe(false)
      expect(localStorageMock.removeItem).toHaveBeenCalledWith('token')
    })
  })
})
