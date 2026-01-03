import { describe, it, expect, beforeEach, vi } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useSessionStore } from '../session'
import { Gender, SessionStatus } from '@/types/session'

// Mock the api module
vi.mock('@/services/api', () => ({
  default: {
    post: vi.fn(),
    get: vi.fn(),
  },
}))

import api from '@/services/api'

const mockSession = {
  id: 'session-123',
  initiatorId: 'user-1',
  partnerId: null,
  targetGender: Gender.Female,
  joinCode: 'ABC123',
  partnerLink: 'unique-link-456',
  status: SessionStatus.WaitingForPartner,
  createdAt: '2024-01-01T00:00:00Z',
  linkedAt: null,
  isInitiator: true,
  partnerDisplayName: null,
  initiatorDisplayName: 'Test User',
}

const mockActiveSession = {
  ...mockSession,
  partnerId: 'user-2',
  status: SessionStatus.Active,
  linkedAt: '2024-01-01T01:00:00Z',
  partnerDisplayName: 'Partner User',
}

const mockName = {
  id: 1,
  nameText: 'Emma',
  gender: Gender.Female,
  popularityScore: 95,
  origin: 'Germanic',
}

describe('Session Store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
  })

  describe('initial state', () => {
    it('should have null session initially', () => {
      const store = useSessionStore()
      expect(store.session).toBeNull()
    })

    it('should have null currentName initially', () => {
      const store = useSessionStore()
      expect(store.currentName).toBeNull()
    })

    it('should not be loading initially', () => {
      const store = useSessionStore()
      expect(store.loading).toBe(false)
    })

    it('should have no error initially', () => {
      const store = useSessionStore()
      expect(store.error).toBeNull()
    })

    it('should have hasSession false initially', () => {
      const store = useSessionStore()
      expect(store.hasSession).toBe(false)
    })
  })

  describe('computed properties', () => {
    it('should return correct hasSession value', async () => {
      vi.mocked(api.post).mockResolvedValueOnce({ data: { data: mockSession } })

      const store = useSessionStore()
      expect(store.hasSession).toBe(false)

      await store.createSession(Gender.Female)
      expect(store.hasSession).toBe(true)
    })

    it('should return correct isWaitingForPartner value', async () => {
      vi.mocked(api.post).mockResolvedValueOnce({ data: { data: mockSession } })

      const store = useSessionStore()
      await store.createSession(Gender.Female)

      expect(store.isWaitingForPartner).toBe(true)
      expect(store.isActive).toBe(false)
    })

    it('should return correct isActive value', async () => {
      vi.mocked(api.post).mockResolvedValueOnce({ data: { data: mockActiveSession } })

      const store = useSessionStore()
      await store.createSession(Gender.Female)

      expect(store.isActive).toBe(true)
      expect(store.isWaitingForPartner).toBe(false)
    })

    it('should return correct isCompleted value', async () => {
      const completedSession = { ...mockSession, status: SessionStatus.Completed }
      vi.mocked(api.post).mockResolvedValueOnce({ data: { data: completedSession } })

      const store = useSessionStore()
      await store.createSession(Gender.Female)

      expect(store.isCompleted).toBe(true)
      expect(store.isActive).toBe(false)
    })

    it('should generate correct shareableLink', async () => {
      vi.mocked(api.post).mockResolvedValueOnce({ data: { data: mockSession } })

      const store = useSessionStore()
      await store.createSession(Gender.Female)

      expect(store.shareableLink).toContain('/join/unique-link-456')
    })

    it('should return null shareableLink when no session', () => {
      const store = useSessionStore()
      expect(store.shareableLink).toBeNull()
    })
  })

  describe('createSession', () => {
    it('should create session successfully', async () => {
      vi.mocked(api.post).mockResolvedValueOnce({ data: { data: mockSession } })

      const store = useSessionStore()
      await store.createSession(Gender.Female)

      expect(api.post).toHaveBeenCalledWith('/sessions', { targetGender: Gender.Female })
      expect(store.session).toEqual(mockSession)
      expect(store.loading).toBe(false)
      expect(store.error).toBeNull()
    })

    it('should set loading state during creation', async () => {
      let resolvePromise: (value: any) => void
      const promise = new Promise((resolve) => {
        resolvePromise = resolve
      })
      vi.mocked(api.post).mockReturnValueOnce(promise as any)

      const store = useSessionStore()
      const createPromise = store.createSession(Gender.Male)

      expect(store.loading).toBe(true)

      resolvePromise!({ data: { data: mockSession } })
      await createPromise

      expect(store.loading).toBe(false)
    })

    it('should handle error on create failure', async () => {
      vi.mocked(api.post).mockRejectedValueOnce({
        response: { data: { errors: ['User already has an active session'] } },
      })

      const store = useSessionStore()
      await expect(store.createSession(Gender.Female)).rejects.toEqual({
        response: { data: { errors: ['User already has an active session'] } },
      })

      expect(store.error).toBe('User already has an active session')
      expect(store.session).toBeNull()
    })

    it('should use fallback error message', async () => {
      vi.mocked(api.post).mockRejectedValueOnce(new Error('Network error'))

      const store = useSessionStore()
      await expect(store.createSession(Gender.Female)).rejects.toThrow()

      expect(store.error).toBe('Failed to create session')
    })
  })

  describe('joinByCode', () => {
    it('should join session by code successfully', async () => {
      vi.mocked(api.post).mockResolvedValueOnce({ data: { data: mockActiveSession } })

      const store = useSessionStore()
      await store.joinByCode('abc123')

      expect(api.post).toHaveBeenCalledWith('/sessions/join', { joinCode: 'ABC123' })
      expect(store.session).toEqual(mockActiveSession)
    })

    it('should convert code to uppercase', async () => {
      vi.mocked(api.post).mockResolvedValueOnce({ data: { data: mockActiveSession } })

      const store = useSessionStore()
      await store.joinByCode('xyz789')

      expect(api.post).toHaveBeenCalledWith('/sessions/join', { joinCode: 'XYZ789' })
    })

    it('should handle error on join failure', async () => {
      vi.mocked(api.post).mockRejectedValueOnce({
        response: { data: { errors: ['Session not found'] } },
      })

      const store = useSessionStore()
      await expect(store.joinByCode('INVALID')).rejects.toBeDefined()

      expect(store.error).toBe('Session not found')
    })
  })

  describe('joinByLink', () => {
    it('should join session by link successfully', async () => {
      vi.mocked(api.get).mockResolvedValueOnce({ data: { data: mockActiveSession } })

      const store = useSessionStore()
      await store.joinByLink('unique-link-456')

      expect(api.get).toHaveBeenCalledWith('/sessions/join/unique-link-456')
      expect(store.session).toEqual(mockActiveSession)
    })

    it('should handle error on link join failure', async () => {
      vi.mocked(api.get).mockRejectedValueOnce({
        response: { data: { errors: ['Invalid link'] } },
      })

      const store = useSessionStore()
      await expect(store.joinByLink('bad-link')).rejects.toBeDefined()

      expect(store.error).toBe('Invalid link')
    })
  })

  describe('fetchCurrentSession', () => {
    it('should fetch current session successfully', async () => {
      vi.mocked(api.get).mockResolvedValueOnce({ data: { data: mockSession } })

      const store = useSessionStore()
      await store.fetchCurrentSession()

      expect(api.get).toHaveBeenCalledWith('/sessions/current')
      expect(store.session).toEqual(mockSession)
    })

    it('should set session to null when no current session', async () => {
      vi.mocked(api.get).mockResolvedValueOnce({ data: { data: null } })

      const store = useSessionStore()
      await store.fetchCurrentSession()

      expect(store.session).toBeNull()
    })

    it('should handle fetch error gracefully', async () => {
      vi.mocked(api.get).mockRejectedValueOnce({
        response: { data: { errors: ['Error fetching session'] } },
      })

      const store = useSessionStore()
      await store.fetchCurrentSession()

      expect(store.session).toBeNull()
      expect(store.error).toBe('Error fetching session')
    })
  })

  describe('refreshSession', () => {
    it('should refresh session when session exists', async () => {
      vi.mocked(api.post).mockResolvedValueOnce({ data: { data: mockSession } })

      const store = useSessionStore()
      await store.createSession(Gender.Female)

      const updatedSession = { ...mockActiveSession }
      vi.mocked(api.get).mockResolvedValueOnce({ data: { data: updatedSession } })

      await store.refreshSession()

      expect(api.get).toHaveBeenCalledWith('/sessions/session-123')
      expect(store.session).toEqual(updatedSession)
    })

    it('should not make request when no session', async () => {
      const store = useSessionStore()
      await store.refreshSession()

      expect(api.get).not.toHaveBeenCalled()
    })

    it('should clear session on refresh error', async () => {
      vi.mocked(api.post).mockResolvedValueOnce({ data: { data: mockSession } })

      const store = useSessionStore()
      await store.createSession(Gender.Female)

      vi.mocked(api.get).mockRejectedValueOnce(new Error('Not found'))

      await store.refreshSession()

      expect(store.session).toBeNull()
    })
  })

  describe('clearSession', () => {
    it('should clear all session-related state', async () => {
      vi.mocked(api.post).mockResolvedValueOnce({ data: { data: mockActiveSession } })

      const store = useSessionStore()
      await store.createSession(Gender.Female)

      // Set some additional state
      store.error = 'Some error'
      store.noMoreNames = true

      store.clearSession()

      expect(store.session).toBeNull()
      expect(store.currentName).toBeNull()
      expect(store.error).toBeNull()
      expect(store.noMoreNames).toBe(false)
    })
  })

  describe('fetchNextName', () => {
    it('should fetch next name when session is active', async () => {
      vi.mocked(api.post).mockResolvedValueOnce({ data: { data: mockActiveSession } })

      const store = useSessionStore()
      await store.createSession(Gender.Female)

      vi.mocked(api.get).mockResolvedValueOnce({ data: { data: mockName } })

      const result = await store.fetchNextName()

      expect(api.get).toHaveBeenCalledWith('/names/next')
      expect(store.currentName).toEqual(mockName)
      expect(result).toEqual(mockName)
      expect(store.noMoreNames).toBe(false)
    })

    it('should return null when no session', async () => {
      const store = useSessionStore()
      const result = await store.fetchNextName()

      expect(result).toBeNull()
      expect(api.get).not.toHaveBeenCalled()
    })

    it('should return null when session is not active', async () => {
      vi.mocked(api.post).mockResolvedValueOnce({ data: { data: mockSession } }) // WaitingForPartner status

      const store = useSessionStore()
      await store.createSession(Gender.Female)

      const result = await store.fetchNextName()

      expect(result).toBeNull()
    })

    it('should set noMoreNames when no name returned', async () => {
      vi.mocked(api.post).mockResolvedValueOnce({ data: { data: mockActiveSession } })

      const store = useSessionStore()
      await store.createSession(Gender.Female)

      vi.mocked(api.get).mockResolvedValueOnce({ data: { data: null } })

      const result = await store.fetchNextName()

      expect(result).toBeNull()
      expect(store.currentName).toBeNull()
      expect(store.noMoreNames).toBe(true)
    })

    it('should handle fetch name error', async () => {
      vi.mocked(api.post).mockResolvedValueOnce({ data: { data: mockActiveSession } })

      const store = useSessionStore()
      await store.createSession(Gender.Female)

      vi.mocked(api.get).mockRejectedValueOnce({
        response: { data: { errors: ['Error fetching name'] } },
      })

      const result = await store.fetchNextName()

      expect(result).toBeNull()
      expect(store.error).toBe('Error fetching name')
    })

    it('should set nameLoading state during fetch', async () => {
      vi.mocked(api.post).mockResolvedValueOnce({ data: { data: mockActiveSession } })

      const store = useSessionStore()
      await store.createSession(Gender.Female)

      let resolvePromise: (value: any) => void
      const promise = new Promise((resolve) => {
        resolvePromise = resolve
      })
      vi.mocked(api.get).mockReturnValueOnce(promise as any)

      const fetchPromise = store.fetchNextName()
      expect(store.nameLoading).toBe(true)

      resolvePromise!({ data: { data: mockName } })
      await fetchPromise

      expect(store.nameLoading).toBe(false)
    })
  })
})
