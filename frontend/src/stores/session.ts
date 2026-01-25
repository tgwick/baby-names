import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { Session, CreateSessionRequest, SessionListItem, SessionListResponse } from '@/types/session'
import type { Name } from '@/types/name'
import type { Match, VoteStats, VoteResult, Conflict } from '@/types/vote'
import { SessionStatus, Gender } from '@/types/session'
import { VoteType } from '@/types/vote'
import api from '@/services/api'

export const useSessionStore = defineStore('session', () => {
  // Multi-session state
  const sessions = ref<SessionListItem[]>([])
  const archivedCount = ref(0)
  const showArchived = ref(false)

  // Current session state
  const session = ref<Session | null>(null)
  const currentName = ref<Name | null>(null)
  const loading = ref(false)
  const nameLoading = ref(false)
  const voteLoading = ref(false)
  const error = ref<string | null>(null)
  const noMoreNames = ref(false)

  // Vote-related state
  const matches = ref<Match[]>([])
  const stats = ref<VoteStats | null>(null)
  const newMatch = ref<Match | null>(null)
  const conflicts = ref<Conflict[]>([])
  const conflictLoading = ref(false)

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

  // Active sessions (non-archived)
  const activeSessions = computed(() => sessions.value.filter((s) => !s.isArchived))

  // Archived sessions
  const archivedSessions = computed(() => sessions.value.filter((s) => s.isArchived))

  // Get session ID (helper for API calls)
  const sessionId = computed(() => session.value?.id || null)

  async function fetchSessions(includeArchived = false) {
    loading.value = true
    error.value = null
    try {
      const response = await api.get('/sessions', { params: { includeArchived } })
      const data: SessionListResponse = response.data.data
      sessions.value = data.sessions
      archivedCount.value = data.archivedCount
    } catch (e: any) {
      error.value = e.response?.data?.errors?.[0] || 'Failed to fetch sessions'
    } finally {
      loading.value = false
    }
  }

  async function setActiveSession(sessionIdToSet: string) {
    loading.value = true
    error.value = null
    try {
      const response = await api.get(`/sessions/${sessionIdToSet}`)
      session.value = response.data.data
      // Clear previous session data
      currentName.value = null
      noMoreNames.value = false
      matches.value = []
      stats.value = null
      conflicts.value = []
    } catch (e: any) {
      error.value = e.response?.data?.errors?.[0] || 'Failed to fetch session'
      throw e
    } finally {
      loading.value = false
    }
  }

  async function archiveSession(sessionIdToArchive: string) {
    loading.value = true
    error.value = null
    try {
      const response = await api.patch(`/sessions/${sessionIdToArchive}/archive`)
      const archivedSession: Session = response.data.data
      // Update the session in the list
      const index = sessions.value.findIndex((s) => s.id === sessionIdToArchive)
      if (index !== -1 && sessions.value[index]) {
        sessions.value[index].isArchived = true
      }
      // Update archivedCount
      archivedCount.value++
      // If the current session was archived, update it
      if (session.value?.id === sessionIdToArchive) {
        session.value = archivedSession
      }
      return archivedSession
    } catch (e: any) {
      error.value = e.response?.data?.errors?.[0] || 'Failed to archive session'
      throw e
    } finally {
      loading.value = false
    }
  }

  async function unarchiveSession(sessionIdToUnarchive: string) {
    loading.value = true
    error.value = null
    try {
      const response = await api.patch(`/sessions/${sessionIdToUnarchive}/unarchive`)
      const unarchivedSession: Session = response.data.data
      // Update the session in the list
      const index = sessions.value.findIndex((s) => s.id === sessionIdToUnarchive)
      if (index !== -1 && sessions.value[index]) {
        sessions.value[index].isArchived = false
      }
      // Update archivedCount
      archivedCount.value = Math.max(0, archivedCount.value - 1)
      // If the current session was unarchived, update it
      if (session.value?.id === sessionIdToUnarchive) {
        session.value = unarchivedSession
      }
      return unarchivedSession
    } catch (e: any) {
      error.value = e.response?.data?.errors?.[0] || 'Failed to unarchive session'
      throw e
    } finally {
      loading.value = false
    }
  }

  async function createSession(targetGender: Gender): Promise<Session> {
    loading.value = true
    error.value = null
    try {
      const request: CreateSessionRequest = { targetGender }
      const response = await api.post('/sessions', request)
      session.value = response.data.data
      // Add to sessions list
      if (session.value) {
        sessions.value.unshift({
          id: session.value.id,
          partnerDisplayName: null,
          createdAt: session.value.createdAt,
          status: session.value.status,
          isArchived: false,
          targetGender: session.value.targetGender,
          matchCount: 0,
          voteCount: 0,
          isInitiator: true,
        })
      }
      return session.value!
    } catch (e: any) {
      error.value = e.response?.data?.errors?.[0] || 'Failed to create session'
      throw e
    } finally {
      loading.value = false
    }
  }

  async function joinByCode(joinCode: string): Promise<Session> {
    loading.value = true
    error.value = null
    try {
      const response = await api.post('/sessions/join', { joinCode: joinCode.toUpperCase() })
      session.value = response.data.data
      return session.value!
    } catch (e: any) {
      error.value = e.response?.data?.errors?.[0] || 'Failed to join session'
      throw e
    } finally {
      loading.value = false
    }
  }

  async function joinByLink(partnerLink: string): Promise<Session> {
    loading.value = true
    error.value = null
    try {
      const response = await api.get(`/sessions/join/${partnerLink}`)
      session.value = response.data.data
      return session.value!
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
    matches.value = []
    stats.value = null
    newMatch.value = null
    conflicts.value = []
  }

  async function fetchNextName() {
    if (!session.value || !isActive.value) return null

    nameLoading.value = true
    error.value = null
    try {
      const response = await api.get('/names/next', { params: { sessionId: session.value.id } })
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

  async function submitVote(nameId: number, voteType: VoteType): Promise<VoteResult | null> {
    if (!session.value || !isActive.value) return null

    voteLoading.value = true
    error.value = null
    newMatch.value = null
    try {
      const response = await api.post('/votes', { sessionId: session.value.id, nameId, voteType })
      const result: VoteResult = response.data.data

      // If it's a match, update matches and set newMatch for celebration
      if (result.isMatch && result.match) {
        matches.value = [result.match, ...matches.value]
        newMatch.value = result.match
      }

      // Update stats
      if (stats.value) {
        stats.value.totalVotes++
        if (voteType === VoteType.Like) {
          stats.value.likeCount++
        } else {
          stats.value.dislikeCount++
        }
        if (result.isMatch) {
          stats.value.matchCount++
        }
        stats.value.namesRemaining = Math.max(0, stats.value.namesRemaining - 1)
      }

      return result
    } catch (e: any) {
      error.value = e.response?.data?.errors?.[0] || 'Failed to submit vote'
      throw e
    } finally {
      voteLoading.value = false
    }
  }

  async function fetchMatches() {
    if (!session.value || !isActive.value) return

    try {
      const response = await api.get('/votes/matches', { params: { sessionId: session.value.id } })
      matches.value = response.data.data || []
    } catch (e: any) {
      error.value = e.response?.data?.errors?.[0] || 'Failed to fetch matches'
    }
  }

  async function fetchStats() {
    if (!session.value || !isActive.value) return

    try {
      const response = await api.get('/votes/stats', { params: { sessionId: session.value.id } })
      stats.value = response.data.data
    } catch (e: any) {
      error.value = e.response?.data?.errors?.[0] || 'Failed to fetch stats'
    }
  }

  function clearNewMatch() {
    newMatch.value = null
  }

  async function fetchConflicts() {
    if (!session.value || !isActive.value) return

    conflictLoading.value = true
    try {
      const response = await api.get('/conflicts', { params: { sessionId: session.value.id } })
      conflicts.value = response.data.data || []
    } catch (e: any) {
      error.value = e.response?.data?.errors?.[0] || 'Failed to fetch conflicts'
    } finally {
      conflictLoading.value = false
    }
  }

  async function clearDislike(nameId: number) {
    if (!session.value || !isActive.value) return false

    conflictLoading.value = true
    error.value = null
    try {
      await api.post(`/conflicts/${nameId}/clear`, null, { params: { sessionId: session.value.id } })
      // Remove the conflict from the list
      conflicts.value = conflicts.value.filter((c) => c.nameId !== nameId)
      return true
    } catch (e: any) {
      error.value = e.response?.data?.errors?.[0] || 'Failed to clear dislike'
      return false
    } finally {
      conflictLoading.value = false
    }
  }

  return {
    // Multi-session state
    sessions,
    archivedCount,
    showArchived,
    // Current session state
    session,
    currentName,
    loading,
    nameLoading,
    voteLoading,
    conflictLoading,
    error,
    noMoreNames,
    matches,
    stats,
    newMatch,
    conflicts,
    // Computed
    hasSession,
    isWaitingForPartner,
    isActive,
    isCompleted,
    shareableLink,
    activeSessions,
    archivedSessions,
    sessionId,
    // Multi-session actions
    fetchSessions,
    setActiveSession,
    archiveSession,
    unarchiveSession,
    // Session actions
    createSession,
    joinByCode,
    joinByLink,
    fetchCurrentSession,
    refreshSession,
    clearSession,
    // Name actions
    fetchNextName,
    // Vote actions
    submitVote,
    fetchMatches,
    fetchStats,
    clearNewMatch,
    // Conflict actions
    fetchConflicts,
    clearDislike,
  }
})
