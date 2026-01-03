import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { Session, CreateSessionRequest } from '@/types/session'
import type { Name } from '@/types/name'
import { SessionStatus, Gender } from '@/types/session'
import api from '@/services/api'

export const useSessionStore = defineStore('session', () => {
  const session = ref<Session | null>(null)
  const currentName = ref<Name | null>(null)
  const loading = ref(false)
  const nameLoading = ref(false)
  const error = ref<string | null>(null)
  const noMoreNames = ref(false)

  const hasSession = computed(() => !!session.value)
  const isWaitingForPartner = computed(
    () => session.value?.status === SessionStatus.WaitingForPartner
  )
  const isActive = computed(() => session.value?.status === SessionStatus.Active)
  const isCompleted = computed(() => session.value?.status === SessionStatus.Completed)

  const shareableLink = computed(() => {
    if (!session.value) return null
    return `${window.location.origin}/join/${session.value.partnerLink}`
  })

  async function createSession(targetGender: Gender) {
    loading.value = true
    error.value = null
    try {
      const request: CreateSessionRequest = { targetGender }
      const response = await api.post('/sessions', request)
      session.value = response.data.data
    } catch (e: any) {
      error.value = e.response?.data?.errors?.[0] || 'Failed to create session'
      throw e
    } finally {
      loading.value = false
    }
  }

  async function joinByCode(joinCode: string) {
    loading.value = true
    error.value = null
    try {
      const response = await api.post('/sessions/join', { joinCode: joinCode.toUpperCase() })
      session.value = response.data.data
    } catch (e: any) {
      error.value = e.response?.data?.errors?.[0] || 'Failed to join session'
      throw e
    } finally {
      loading.value = false
    }
  }

  async function joinByLink(partnerLink: string) {
    loading.value = true
    error.value = null
    try {
      const response = await api.get(`/sessions/join/${partnerLink}`)
      session.value = response.data.data
    } catch (e: any) {
      error.value = e.response?.data?.errors?.[0] || 'Failed to join session'
      throw e
    } finally {
      loading.value = false
    }
  }

  async function fetchCurrentSession() {
    loading.value = true
    error.value = null
    try {
      const response = await api.get('/sessions/current')
      session.value = response.data.data || null
    } catch (e: any) {
      error.value = e.response?.data?.errors?.[0] || 'Failed to fetch session'
      session.value = null
    } finally {
      loading.value = false
    }
  }

  async function refreshSession() {
    if (!session.value) return
    try {
      const response = await api.get(`/sessions/${session.value.id}`)
      session.value = response.data.data
    } catch {
      // If session not found, clear it
      session.value = null
    }
  }

  function clearSession() {
    session.value = null
    currentName.value = null
    error.value = null
    noMoreNames.value = false
  }

  async function fetchNextName() {
    if (!session.value || !isActive.value) return null

    nameLoading.value = true
    error.value = null
    try {
      const response = await api.get('/names/next')
      if (response.data.data) {
        currentName.value = response.data.data
        noMoreNames.value = false
        return currentName.value
      } else {
        currentName.value = null
        noMoreNames.value = true
        return null
      }
    } catch (e: any) {
      error.value = e.response?.data?.errors?.[0] || 'Failed to fetch name'
      return null
    } finally {
      nameLoading.value = false
    }
  }

  return {
    session,
    currentName,
    loading,
    nameLoading,
    error,
    noMoreNames,
    hasSession,
    isWaitingForPartner,
    isActive,
    isCompleted,
    shareableLink,
    createSession,
    joinByCode,
    joinByLink,
    fetchCurrentSession,
    refreshSession,
    clearSession,
    fetchNextName,
  }
})
