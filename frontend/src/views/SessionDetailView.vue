<script setup lang="ts">
import { onMounted, computed, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useSessionStore } from '@/stores/session'
import { Gender } from '@/types/session'

const route = useRoute()
const router = useRouter()
const sessionStore = useSessionStore()

const archiving = ref(false)
const copied = ref(false)

const sessionId = computed(() => route.params.sessionId as string)

const partnerName = computed(() => {
  if (!sessionStore.session) return ''
  return sessionStore.session.isInitiator
    ? sessionStore.session.partnerDisplayName
    : sessionStore.session.initiatorDisplayName
})

const genderLabel = computed(() => {
  if (!sessionStore.session) return ''
  switch (sessionStore.session.targetGender) {
    case Gender.Male:
      return 'Boy names'
    case Gender.Female:
      return 'Girl names'
    case Gender.Neutral:
      return 'Any gender'
    default:
      return ''
  }
})

const genderEmoji = computed(() => {
  if (!sessionStore.session) return '🌟'
  switch (sessionStore.session.targetGender) {
    case Gender.Male:
      return '👦'
    case Gender.Female:
      return '👧'
    case Gender.Neutral:
      return '🌟'
    default:
      return '🌟'
  }
})

const canSwipe = computed(() => {
  return sessionStore.session?.canStartVoting && sessionStore.isActive
})

const filtersCompleted = computed(() => {
  if (!sessionStore.session) return false
  return sessionStore.session.isInitiator
    ? sessionStore.session.initiatorFiltersCompleted
    : sessionStore.session.partnerFiltersCompleted
})

const partnerFiltersCompleted = computed(() => {
  if (!sessionStore.session) return false
  return sessionStore.session.isInitiator
    ? sessionStore.session.partnerFiltersCompleted
    : sessionStore.session.initiatorFiltersCompleted
})

onMounted(async () => {
  await sessionStore.setActiveSession(sessionId.value)

  if (sessionStore.isActive) {
    await Promise.all([
      sessionStore.fetchMatches(),
      sessionStore.fetchStats(),
      sessionStore.fetchConflicts(),
    ])
  }
})

async function copyJoinCode() {
  if (!sessionStore.session) return
  try {
    await navigator.clipboard.writeText(sessionStore.session.joinCode)
    copied.value = true
    setTimeout(() => {
      copied.value = false
    }, 2000)
  } catch {
    // Fallback for browsers that don't support clipboard API
    const textArea = document.createElement('textarea')
    textArea.value = sessionStore.session.joinCode
    document.body.appendChild(textArea)
    textArea.select()
    document.execCommand('copy')
    document.body.removeChild(textArea)
    copied.value = true
    setTimeout(() => {
      copied.value = false
    }, 2000)
  }
}

async function copyShareLink() {
  if (!sessionStore.shareableLink) return
  try {
    await navigator.clipboard.writeText(sessionStore.shareableLink)
    copied.value = true
    setTimeout(() => {
      copied.value = false
    }, 2000)
  } catch {
    // Fallback
  }
}

async function handleArchive() {
  if (!sessionStore.session) return
  archiving.value = true
  try {
    if (sessionStore.session.isArchived) {
      await sessionStore.unarchiveSession(sessionStore.session.id)
    } else {
      await sessionStore.archiveSession(sessionStore.session.id)
    }
  } finally {
    archiving.value = false
  }
}

function goToSwipe() {
  router.push(`/sessions/${sessionId.value}/swipe`)
}

function goToMatches() {
  router.push(`/sessions/${sessionId.value}/matches`)
}

function goToConflicts() {
  router.push(`/sessions/${sessionId.value}/conflicts`)
}

function goToPreferences() {
  router.push(`/sessions/${sessionId.value}/preferences`)
}

function goBack() {
  router.push('/sessions')
}

function formatDate(dateString: string | null): string {
  if (!dateString) return ''
  const date = new Date(dateString)
  return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })
}
</script>

<template>
  <div class="max-w-4xl mx-auto">
    <!-- Back Button -->
    <button
      @click="goBack"
      class="flex items-center gap-2 text-[var(--color-warm-gray-light)] hover:text-[var(--color-warm-gray)] mb-4 transition-colors"
    >
      <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
        <path fill-rule="evenodd" d="M12.707 5.293a1 1 0 010 1.414L9.414 10l3.293 3.293a1 1 0 01-1.414 1.414l-4-4a1 1 0 010-1.414l4-4a1 1 0 011.414 0z" clip-rule="evenodd" />
      </svg>
      <span>All Sessions</span>
    </button>

    <!-- Loading State -->
    <div v-if="sessionStore.loading" class="text-center py-12">
      <div class="inline-block animate-spin text-4xl mb-4">🥚</div>
      <p class="text-[var(--color-warm-gray-light)]">Loading session...</p>
    </div>

    <!-- Error State -->
    <div v-else-if="sessionStore.error" class="card p-6 text-center">
      <div class="text-4xl mb-4">😕</div>
      <p class="text-[var(--color-warm-gray)]">{{ sessionStore.error }}</p>
      <button @click="goBack" class="btn-secondary mt-4">Go Back</button>
    </div>

    <!-- Session Content -->
    <template v-else-if="sessionStore.session">
      <!-- Session Header -->
      <div
        class="card-elevated mb-6 overflow-hidden animate-slide-up"
        :class="{ 'opacity-75': sessionStore.session.isArchived }"
      >
        <div
          class="p-4 sm:p-6"
          :class="sessionStore.isWaitingForPartner
            ? 'bg-gradient-to-r from-[var(--color-blush)] to-[var(--color-peach-light)]'
            : 'bg-gradient-to-r from-[var(--color-mint)] to-[#C8E6DC]'"
        >
          <div class="flex items-center gap-4">
            <div
              class="w-14 h-14 sm:w-16 sm:h-16 rounded-2xl flex items-center justify-center text-2xl sm:text-3xl flex-shrink-0"
              :class="sessionStore.isWaitingForPartner ? 'bg-white/50' : 'bg-white/60'"
            >
              {{ genderEmoji }}
            </div>
            <div class="flex-grow min-w-0">
              <div class="flex items-center gap-2 flex-wrap">
                <h1 class="font-display text-xl sm:text-2xl font-semibold" :class="sessionStore.isWaitingForPartner ? 'text-[var(--color-coral)]' : 'text-emerald-700'">
                  {{ sessionStore.isWaitingForPartner ? 'Waiting for partner' : partnerName }}
                </h1>
                <span v-if="sessionStore.session.isArchived" class="text-xs px-2 py-0.5 rounded-full bg-gray-200 text-gray-500">
                  Archived
                </span>
              </div>
              <div class="flex items-center gap-2 mt-1 text-sm" :class="sessionStore.isWaitingForPartner ? 'text-[var(--color-coral)]/80' : 'text-emerald-600'">
                <span>{{ genderLabel }}</span>
                <span>•</span>
                <span>{{ formatDate(sessionStore.session.createdAt) }}</span>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Waiting for Partner - Share Section -->
      <div
        v-if="sessionStore.isWaitingForPartner"
        class="card p-4 sm:p-6 mb-6 animate-slide-up stagger-1"
        style="animation-fill-mode: forwards; opacity: 0;"
      >
        <h2 class="font-display text-lg font-semibold text-[var(--color-warm-gray)] mb-4">
          Share with your partner
        </h2>

        <!-- Join Code -->
        <div class="mb-4">
          <label class="text-sm text-[var(--color-warm-gray-light)] mb-2 block">Join Code</label>
          <div class="flex items-center gap-2">
            <div class="font-mono text-2xl font-bold text-[var(--color-warm-gray)] bg-[var(--color-cream)] px-4 py-2 rounded-lg flex-grow text-center tracking-wider">
              {{ sessionStore.session.joinCode }}
            </div>
            <button @click="copyJoinCode" class="btn-secondary">
              {{ copied ? 'Copied!' : 'Copy' }}
            </button>
          </div>
        </div>

        <!-- Share Link -->
        <div>
          <label class="text-sm text-[var(--color-warm-gray-light)] mb-2 block">Or share this link</label>
          <div class="flex items-center gap-2">
            <input
              type="text"
              :value="sessionStore.shareableLink"
              readonly
              class="input flex-grow text-sm"
            />
            <button @click="copyShareLink" class="btn-secondary">
              {{ copied ? 'Copied!' : 'Copy' }}
            </button>
          </div>
        </div>
      </div>

      <!-- Preferences while waiting for partner -->
      <div
        v-if="sessionStore.isWaitingForPartner"
        class="card p-4 sm:p-6 mb-6 animate-slide-up stagger-2"
        style="animation-fill-mode: forwards; opacity: 0;"
      >
        <h2 class="font-display text-lg font-semibold text-[var(--color-warm-gray)] mb-4">
          Set your preferences while you wait
        </h2>
        <p class="text-sm text-[var(--color-warm-gray-light)] mb-4">
          Get a head start by setting your name preferences now. You'll be ready to swipe as soon as your partner joins!
        </p>

        <div class="flex items-center gap-2 mb-4">
          <span :class="filtersCompleted ? 'text-emerald-500' : 'text-gray-300'">
            {{ filtersCompleted ? '✓' : '○' }}
          </span>
          <span class="text-sm">{{ filtersCompleted ? 'Preferences set' : 'Not yet set' }}</span>
        </div>

        <button
          v-if="!filtersCompleted"
          @click="goToPreferences"
          class="btn-primary"
        >
          <span>Set Preferences</span>
        </button>
        <button
          v-else
          @click="goToPreferences"
          class="btn-secondary"
        >
          <span>Update Preferences</span>
        </button>
      </div>

      <!-- Filter Status (when partner has joined but can't swipe yet) -->
      <div
        v-if="sessionStore.isActive && !canSwipe"
        class="card p-4 sm:p-6 mb-6 animate-slide-up stagger-1"
        style="animation-fill-mode: forwards; opacity: 0;"
      >
        <h2 class="font-display text-lg font-semibold text-[var(--color-warm-gray)] mb-4">
          Complete your preferences
        </h2>
        <p class="text-sm text-[var(--color-warm-gray-light)] mb-4">
          Both partners need to complete their preferences before you can start swiping.
        </p>

        <div class="flex items-center gap-4 mb-4">
          <div class="flex items-center gap-2">
            <span :class="filtersCompleted ? 'text-emerald-500' : 'text-gray-300'">
              {{ filtersCompleted ? '✓' : '○' }}
            </span>
            <span class="text-sm">You</span>
          </div>
          <div class="flex items-center gap-2">
            <span :class="partnerFiltersCompleted ? 'text-emerald-500' : 'text-gray-300'">
              {{ partnerFiltersCompleted ? '✓' : '○' }}
            </span>
            <span class="text-sm">{{ partnerName || 'Partner' }}</span>
          </div>
        </div>

        <button
          v-if="!filtersCompleted"
          @click="goToPreferences"
          class="btn-primary"
        >
          <span>Set Preferences</span>
        </button>
        <p v-else class="text-sm text-[var(--color-warm-gray-light)]">
          Waiting for {{ partnerName || 'your partner' }} to complete their preferences...
        </p>
      </div>

      <!-- Action Cards (when can swipe) -->
      <div v-if="canSwipe" class="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6">
        <!-- Swipe Card -->
        <div
          class="card p-4 sm:p-5 hover:shadow-lg transition-all duration-300 animate-slide-up stagger-1"
          style="animation-fill-mode: forwards; opacity: 0;"
        >
          <div class="w-12 h-12 rounded-xl bg-[var(--color-blush)] flex items-center justify-center text-2xl mb-3">
            💕
          </div>
          <h3 class="font-display text-lg font-semibold text-[var(--color-warm-gray)] mb-2">Swipe Names</h3>
          <p class="text-sm text-[var(--color-warm-gray-light)] mb-4">
            Swipe through names to find your favorites.
          </p>
          <button @click="goToSwipe" class="btn-primary w-full">
            <span>Start Swiping</span>
          </button>
        </div>

        <!-- Matches Card -->
        <div
          class="card p-4 sm:p-5 hover:shadow-lg transition-all duration-300 animate-slide-up stagger-2"
          style="animation-fill-mode: forwards; opacity: 0;"
        >
          <div class="w-12 h-12 rounded-xl bg-[var(--color-mint)] flex items-center justify-center text-2xl mb-3">
            🎉
          </div>
          <h3 class="font-display text-lg font-semibold text-[var(--color-warm-gray)] mb-2">
            Matches
            <span v-if="sessionStore.stats?.matchCount" class="text-[var(--color-warm-gray-light)]">
              ({{ sessionStore.stats.matchCount }})
            </span>
          </h3>
          <p class="text-sm text-[var(--color-warm-gray-light)] mb-4">
            Names you both loved!
          </p>
          <button @click="goToMatches" class="btn-secondary w-full">
            View Matches
          </button>
        </div>

        <!-- Conflicts Card -->
        <div
          class="card p-4 sm:p-5 hover:shadow-lg transition-all duration-300 animate-slide-up stagger-3"
          style="animation-fill-mode: forwards; opacity: 0;"
        >
          <div class="w-12 h-12 rounded-xl bg-[var(--color-lavender)] flex items-center justify-center text-2xl mb-3">
            🤔
          </div>
          <h3 class="font-display text-lg font-semibold text-[var(--color-warm-gray)] mb-2">
            Conflicts
            <span v-if="sessionStore.conflicts.length" class="text-[var(--color-warm-gray-light)]">
              ({{ sessionStore.conflicts.length }})
            </span>
          </h3>
          <p class="text-sm text-[var(--color-warm-gray-light)] mb-4">
            Names to reconsider together.
          </p>
          <button @click="goToConflicts" class="btn-secondary w-full">
            View Conflicts
          </button>
        </div>
      </div>

      <!-- Stats (when active) -->
      <div
        v-if="sessionStore.isActive && sessionStore.stats"
        class="card p-4 sm:p-6 mb-6 animate-slide-up stagger-4"
        style="animation-fill-mode: forwards; opacity: 0;"
      >
        <h2 class="font-display text-lg font-semibold text-[var(--color-warm-gray)] mb-4">
          Your Progress
        </h2>
        <div class="grid grid-cols-2 sm:grid-cols-4 gap-4">
          <div class="text-center">
            <p class="font-display text-2xl font-semibold text-[var(--color-warm-gray)]">{{ sessionStore.stats.totalVotes }}</p>
            <p class="text-xs text-[var(--color-warm-gray-light)]">Names Voted</p>
          </div>
          <div class="text-center">
            <p class="font-display text-2xl font-semibold text-emerald-600">{{ sessionStore.stats.likeCount }}</p>
            <p class="text-xs text-[var(--color-warm-gray-light)]">Likes</p>
          </div>
          <div class="text-center">
            <p class="font-display text-2xl font-semibold text-[var(--color-coral)]">{{ sessionStore.stats.dislikeCount }}</p>
            <p class="text-xs text-[var(--color-warm-gray-light)]">Dislikes</p>
          </div>
          <div class="text-center">
            <p class="font-display text-2xl font-semibold text-[var(--color-warm-gray)]">{{ sessionStore.stats.namesRemaining }}</p>
            <p class="text-xs text-[var(--color-warm-gray-light)]">Remaining</p>
          </div>
        </div>
      </div>

      <!-- Archive Button -->
      <div
        class="text-center animate-slide-up stagger-5"
        style="animation-fill-mode: forwards; opacity: 0;"
      >
        <button
          @click="handleArchive"
          :disabled="archiving"
          class="text-sm text-[var(--color-warm-gray-light)] hover:text-[var(--color-warm-gray)] transition-colors"
        >
          {{ archiving ? 'Processing...' : (sessionStore.session.isArchived ? 'Unarchive Session' : 'Archive Session') }}
        </button>
      </div>
    </template>
  </div>
</template>
