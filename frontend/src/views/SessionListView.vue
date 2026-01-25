<script setup lang="ts">
import { onMounted, ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useSessionStore } from '@/stores/session'
import { SessionStatus, Gender } from '@/types/session'

const router = useRouter()
const authStore = useAuthStore()
const sessionStore = useSessionStore()

const showArchived = ref(false)

const displayedSessions = computed(() => {
  return showArchived.value ? sessionStore.sessions : sessionStore.activeSessions
})

const genderLabel = (gender: Gender): string => {
  switch (gender) {
    case Gender.Male:
      return 'Boy'
    case Gender.Female:
      return 'Girl'
    case Gender.Neutral:
      return 'Any'
    default:
      return ''
  }
}

const genderEmoji = (gender: Gender): string => {
  switch (gender) {
    case Gender.Male:
      return '👦'
    case Gender.Female:
      return '👧'
    case Gender.Neutral:
      return '🌟'
    default:
      return ''
  }
}

const statusLabel = (status: SessionStatus): string => {
  switch (status) {
    case SessionStatus.WaitingForPartner:
      return 'Waiting'
    case SessionStatus.Active:
      return 'Active'
    case SessionStatus.Completed:
      return 'Completed'
    default:
      return ''
  }
}

const statusColor = (status: SessionStatus): string => {
  switch (status) {
    case SessionStatus.WaitingForPartner:
      return 'bg-amber-100 text-amber-700'
    case SessionStatus.Active:
      return 'bg-emerald-100 text-emerald-700'
    case SessionStatus.Completed:
      return 'bg-gray-100 text-gray-600'
    default:
      return ''
  }
}

onMounted(async () => {
  await sessionStore.fetchSessions(true)

  // Check for pending join link
  const pendingLink = localStorage.getItem('pendingJoinLink')
  if (pendingLink) {
    localStorage.removeItem('pendingJoinLink')
    router.push(`/join/${pendingLink}`)
  }
})

async function toggleShowArchived() {
  showArchived.value = !showArchived.value
}

function goToSession(sessionId: string) {
  router.push(`/sessions/${sessionId}`)
}

function formatDate(dateString: string): string {
  const date = new Date(dateString)
  return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })
}
</script>

<template>
  <div class="max-w-4xl mx-auto">
    <!-- Welcome Header -->
    <div class="mb-6 sm:mb-8 animate-slide-up">
      <h1 class="font-display text-2xl sm:text-3xl md:text-4xl font-semibold text-[var(--color-warm-gray)]">
        Welcome back{{ authStore.user?.displayName ? `, ${authStore.user.displayName}` : '' }}!
      </h1>
      <p class="text-sm sm:text-base text-[var(--color-warm-gray-light)] mt-1.5 sm:mt-2">
        Your name-hatching sessions
      </p>
    </div>

    <!-- Action Buttons -->
    <div class="flex flex-wrap gap-3 mb-6 sm:mb-8 animate-slide-up stagger-1" style="animation-fill-mode: forwards; opacity: 0;">
      <RouterLink to="/session/create" class="btn-primary text-sm sm:text-base">
        <span>+ New Session</span>
      </RouterLink>
      <RouterLink to="/session/join" class="btn-secondary text-sm sm:text-base">
        Join Session
      </RouterLink>
      <button
        v-if="sessionStore.archivedCount > 0"
        @click="toggleShowArchived"
        class="btn-secondary text-sm sm:text-base"
      >
        {{ showArchived ? 'Hide Archived' : `Show Archived (${sessionStore.archivedCount})` }}
      </button>
    </div>

    <!-- Loading State -->
    <div v-if="sessionStore.loading" class="text-center py-12">
      <div class="inline-block animate-spin text-4xl mb-4">🥚</div>
      <p class="text-[var(--color-warm-gray-light)]">Loading sessions...</p>
    </div>

    <!-- Sessions List -->
    <div v-else-if="displayedSessions.length > 0" class="space-y-4">
      <div
        v-for="(session, index) in displayedSessions"
        :key="session.id"
        class="card p-4 sm:p-5 hover:shadow-lg transition-all duration-300 cursor-pointer animate-slide-up"
        :class="{ 'opacity-60': session.isArchived }"
        :style="{ animationDelay: `${(index + 2) * 100}ms`, animationFillMode: 'forwards', opacity: 0 }"
        @click="goToSession(session.id)"
      >
        <div class="flex items-center justify-between gap-4">
          <!-- Session Info -->
          <div class="flex items-center gap-3 sm:gap-4 min-w-0">
            <div class="w-11 h-11 sm:w-14 sm:h-14 rounded-xl sm:rounded-2xl bg-[var(--color-blush)] flex items-center justify-center text-xl sm:text-2xl flex-shrink-0">
              {{ genderEmoji(session.targetGender) }}
            </div>
            <div class="min-w-0">
              <div class="flex items-center gap-2 flex-wrap">
                <p class="font-display text-base sm:text-lg font-semibold text-[var(--color-warm-gray)] truncate">
                  {{ session.partnerDisplayName || 'Waiting for partner...' }}
                </p>
                <span
                  class="text-xs px-2 py-0.5 rounded-full flex-shrink-0"
                  :class="statusColor(session.status)"
                >
                  {{ statusLabel(session.status) }}
                </span>
                <span v-if="session.isArchived" class="text-xs px-2 py-0.5 rounded-full bg-gray-200 text-gray-500 flex-shrink-0">
                  Archived
                </span>
              </div>
              <div class="flex items-center gap-2 sm:gap-3 text-xs sm:text-sm text-[var(--color-warm-gray-light)] mt-0.5">
                <span>{{ genderLabel(session.targetGender) }} names</span>
                <span>•</span>
                <span>{{ formatDate(session.createdAt) }}</span>
              </div>
            </div>
          </div>

          <!-- Stats -->
          <div class="flex items-center gap-3 sm:gap-4 flex-shrink-0">
            <div class="text-center hidden sm:block">
              <p class="font-display text-lg font-semibold text-[var(--color-warm-gray)]">{{ session.matchCount }}</p>
              <p class="text-xs text-[var(--color-warm-gray-light)]">matches</p>
            </div>
            <div class="text-center hidden sm:block">
              <p class="font-display text-lg font-semibold text-[var(--color-warm-gray)]">{{ session.voteCount }}</p>
              <p class="text-xs text-[var(--color-warm-gray-light)]">votes</p>
            </div>
            <div class="text-[var(--color-warm-gray-light)]">
              <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M7.293 14.707a1 1 0 010-1.414L10.586 10 7.293 6.707a1 1 0 011.414-1.414l4 4a1 1 0 010 1.414l-4 4a1 1 0 01-1.414 0z" clip-rule="evenodd" />
              </svg>
            </div>
          </div>
        </div>

        <!-- Mobile Stats -->
        <div class="flex items-center gap-4 mt-3 sm:hidden text-sm text-[var(--color-warm-gray-light)]">
          <span>{{ session.matchCount }} matches</span>
          <span>•</span>
          <span>{{ session.voteCount }} votes</span>
        </div>
      </div>
    </div>

    <!-- Empty State -->
    <div
      v-else
      class="card p-6 sm:p-8 text-center animate-slide-up stagger-2"
      style="animation-fill-mode: forwards; opacity: 0;"
    >
      <div class="text-5xl sm:text-6xl mb-4">🥚</div>
      <h2 class="font-display text-xl sm:text-2xl font-semibold text-[var(--color-warm-gray)] mb-2">
        No sessions yet
      </h2>
      <p class="text-sm sm:text-base text-[var(--color-warm-gray-light)] mb-6 max-w-md mx-auto">
        Start a new session and invite your partner to begin hatching the perfect name together!
      </p>
      <div class="flex justify-center gap-3">
        <RouterLink to="/session/create" class="btn-primary">
          <span>Create Session</span>
        </RouterLink>
        <RouterLink to="/session/join" class="btn-secondary">
          Join Session
        </RouterLink>
      </div>
    </div>
  </div>
</template>
