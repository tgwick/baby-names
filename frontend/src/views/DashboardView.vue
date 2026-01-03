<script setup lang="ts">
import { onMounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useSessionStore } from '@/stores/session'

const router = useRouter()
const authStore = useAuthStore()
const sessionStore = useSessionStore()

const partnerName = computed(() => {
  if (!sessionStore.session) return ''
  return sessionStore.session.isInitiator
    ? sessionStore.session.partnerDisplayName
    : sessionStore.session.initiatorDisplayName
})

const matchCount = computed(() => sessionStore.stats?.matchCount ?? sessionStore.matches.length)

onMounted(async () => {
  await sessionStore.fetchCurrentSession()

  const pendingLink = localStorage.getItem('pendingJoinLink')
  if (pendingLink) {
    localStorage.removeItem('pendingJoinLink')
    router.push(`/join/${pendingLink}`)
  }

  // Fetch matches and stats if session is active
  if (sessionStore.isActive) {
    await Promise.all([
      sessionStore.fetchMatches(),
      sessionStore.fetchStats(),
    ])
  }
})

function goToSession() {
  router.push('/session')
}

function goToSwipe() {
  router.push('/swipe')
}

function goToMatches() {
  router.push('/matches')
}
</script>

<template>
  <div class="max-w-4xl mx-auto">
    <!-- Welcome Header -->
    <div class="mb-8 animate-slide-up">
      <h1 class="font-display text-4xl font-semibold text-[var(--color-warm-gray)]">
        Welcome back{{ authStore.user?.displayName ? `, ${authStore.user.displayName}` : '' }}!
      </h1>
      <p class="text-[var(--color-warm-gray-light)] mt-2">
        Let's find the perfect name together.
      </p>
    </div>

    <!-- Active Session Banner -->
    <div
      v-if="sessionStore.hasSession"
      class="card-elevated mb-8 overflow-hidden animate-slide-up stagger-1"
      style="animation-fill-mode: forwards; opacity: 0;"
    >
      <div
        class="p-6"
        :class="sessionStore.isWaitingForPartner
          ? 'bg-gradient-to-r from-[var(--color-blush)] to-[var(--color-peach-light)]'
          : 'bg-gradient-to-r from-[var(--color-mint)] to-[#C8E6DC]'"
      >
        <div class="flex items-center justify-between flex-wrap gap-4">
          <div class="flex items-center gap-4">
            <div
              class="w-14 h-14 rounded-2xl flex items-center justify-center text-2xl"
              :class="sessionStore.isWaitingForPartner ? 'bg-white/50' : 'bg-white/60'"
            >
              {{ sessionStore.isWaitingForPartner ? '⏳' : '💑' }}
            </div>
            <div>
              <p class="font-display text-lg font-semibold" :class="sessionStore.isWaitingForPartner ? 'text-[var(--color-coral)]' : 'text-emerald-700'">
                {{ sessionStore.isWaitingForPartner ? 'Waiting for partner' : 'Session Active!' }}
              </p>
              <p class="text-sm" :class="sessionStore.isWaitingForPartner ? 'text-[var(--color-coral)]/80' : 'text-emerald-600'">
                {{ sessionStore.isWaitingForPartner
                  ? `Share code: ${sessionStore.session?.joinCode}`
                  : `Connected with ${partnerName}`
                }}
              </p>
            </div>
          </div>
          <button
            @click="goToSession"
            class="btn-primary"
          >
            <span>{{ sessionStore.isWaitingForPartner ? 'Share Code' : 'Continue' }} →</span>
          </button>
        </div>
      </div>
    </div>

    <!-- Action Cards -->
    <div class="grid grid-cols-1 md:grid-cols-2 gap-6 mb-8">
      <!-- Create Session Card -->
      <div
        class="card p-6 hover:shadow-lg transition-all duration-300 animate-slide-up stagger-2"
        style="animation-fill-mode: forwards; opacity: 0;"
        :class="{ 'opacity-60': sessionStore.hasSession }"
      >
        <div class="w-14 h-14 rounded-2xl bg-[var(--color-blush)] flex items-center justify-center text-2xl mb-4">
          ✨
        </div>
        <h2 class="font-display text-xl font-semibold text-[var(--color-warm-gray)] mb-2">
          Start a New Session
        </h2>
        <p class="text-[var(--color-warm-gray-light)] mb-6 text-sm leading-relaxed">
          Create a session and invite your partner to swipe through baby names together.
        </p>
        <RouterLink
          to="/session/create"
          :class="[
            'btn-primary block text-center',
            sessionStore.hasSession ? 'opacity-50 pointer-events-none' : ''
          ]"
        >
          <span>{{ sessionStore.hasSession ? 'Session Active' : 'Create Session' }}</span>
        </RouterLink>
      </div>

      <!-- Join Session Card -->
      <div
        class="card p-6 hover:shadow-lg transition-all duration-300 animate-slide-up stagger-3"
        style="animation-fill-mode: forwards; opacity: 0;"
        :class="{ 'opacity-60': sessionStore.hasSession }"
      >
        <div class="w-14 h-14 rounded-2xl bg-[var(--color-lavender)] flex items-center justify-center text-2xl mb-4">
          🔗
        </div>
        <h2 class="font-display text-xl font-semibold text-[var(--color-warm-gray)] mb-2">
          Join a Session
        </h2>
        <p class="text-[var(--color-warm-gray-light)] mb-6 text-sm leading-relaxed">
          Have a code from your partner? Enter it to join their session and start matching.
        </p>
        <RouterLink
          to="/session/join"
          :class="[
            'btn-secondary block text-center',
            sessionStore.hasSession ? 'opacity-50 pointer-events-none' : ''
          ]"
        >
          {{ sessionStore.hasSession ? 'Session Active' : 'Enter Code' }}
        </RouterLink>
      </div>
    </div>

    <!-- Matches Preview Card -->
    <div
      class="card p-6 animate-slide-up stagger-4"
      style="animation-fill-mode: forwards; opacity: 0;"
    >
      <div class="flex items-center justify-between mb-4">
        <div class="flex items-center gap-4">
          <div class="w-12 h-12 rounded-xl bg-[var(--color-sky)] flex items-center justify-center text-xl">
            💕
          </div>
          <div>
            <h2 class="font-display text-xl font-semibold text-[var(--color-warm-gray)]">Your Matches</h2>
            <p class="text-sm text-[var(--color-warm-gray-light)]">Names you both loved</p>
          </div>
        </div>
        <button
          v-if="matchCount > 0"
          @click="goToMatches"
          class="btn-secondary text-sm py-2 px-4"
        >
          View All
        </button>
      </div>

      <!-- Has matches -->
      <div v-if="matchCount > 0" class="space-y-3">
        <div
          v-for="match in sessionStore.matches.slice(0, 3)"
          :key="match.nameId"
          class="flex items-center gap-3 p-3 bg-[var(--color-cream)] rounded-xl"
        >
          <span class="text-2xl">💗</span>
          <span class="font-display text-lg font-semibold text-[var(--color-warm-gray)]">{{ match.nameText }}</span>
          <span v-if="match.origin" class="text-sm text-[var(--color-warm-gray-light)]">• {{ match.origin }}</span>
        </div>
        <p v-if="matchCount > 3" class="text-center text-sm text-[var(--color-warm-gray-light)]">
          +{{ matchCount - 3 }} more matches
        </p>
      </div>

      <!-- No matches yet -->
      <div v-else class="bg-[var(--color-cream)] rounded-xl p-6 text-center">
        <div class="text-4xl mb-3">💝</div>
        <p class="text-[var(--color-warm-gray-light)]">
          {{ sessionStore.hasSession && sessionStore.isActive
            ? 'No matches yet. Start swiping to find names you both love!'
            : 'Start a session to begin discovering the perfect name together.'
          }}
        </p>
        <button
          v-if="sessionStore.hasSession && sessionStore.isActive"
          @click="goToSwipe"
          class="btn-primary mt-4"
        >
          <span>Start Swiping</span>
        </button>
      </div>
    </div>
  </div>
</template>
