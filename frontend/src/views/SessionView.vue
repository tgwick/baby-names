<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useSessionStore } from '@/stores/session'
import { Gender } from '@/types/session'

const router = useRouter()
const sessionStore = useSessionStore()

const copied = ref<'code' | 'link' | null>(null)
let refreshInterval: number | null = null

const genderLabel = computed(() => {
  switch (sessionStore.session?.targetGender) {
    case Gender.Male:
      return 'Boy Names'
    case Gender.Female:
      return 'Girl Names'
    default:
      return 'All Names'
  }
})

const genderIcon = computed(() => {
  switch (sessionStore.session?.targetGender) {
    case Gender.Male:
      return '👦'
    case Gender.Female:
      return '👧'
    default:
      return '👶'
  }
})

const partnerName = computed(() => {
  if (!sessionStore.session) return ''
  return sessionStore.session.isInitiator
    ? sessionStore.session.partnerDisplayName
    : sessionStore.session.initiatorDisplayName
})

// Filter/Preference-related computed (uses new filters system with legacy fallback)
const myPrefsCompleted = computed(() => {
  if (!sessionStore.session) return false
  const s = sessionStore.session
  // Use filters (new system) or fall back to legacy prefs
  return s.isInitiator
    ? (s.initiatorFiltersCompleted || s.initiatorPrefsCompleted)
    : (s.partnerFiltersCompleted || s.partnerPrefsCompleted)
})

const partnerPrefsCompleted = computed(() => {
  if (!sessionStore.session) return false
  const s = sessionStore.session
  // Use filters (new system) or fall back to legacy prefs
  return s.isInitiator
    ? (s.partnerFiltersCompleted || s.partnerPrefsCompleted)
    : (s.initiatorFiltersCompleted || s.initiatorPrefsCompleted)
})

const canStartVoting = computed(() => sessionStore.session?.canStartVoting ?? false)

async function copyCode() {
  if (!sessionStore.session) return
  await navigator.clipboard.writeText(sessionStore.session.joinCode)
  copied.value = 'code'
  setTimeout(() => (copied.value = null), 2000)
}

async function copyLink() {
  if (!sessionStore.shareableLink) return
  await navigator.clipboard.writeText(sessionStore.shareableLink)
  copied.value = 'link'
  setTimeout(() => (copied.value = null), 2000)
}

onMounted(async () => {
  await sessionStore.fetchCurrentSession()

  if (!sessionStore.hasSession) {
    router.push('/dashboard')
    return
  }

  if (sessionStore.isWaitingForPartner) {
    refreshInterval = window.setInterval(async () => {
      await sessionStore.refreshSession()
      if (sessionStore.isActive && refreshInterval) {
        clearInterval(refreshInterval)
        refreshInterval = null
      }
    }, 5000)
  }
})

onUnmounted(() => {
  if (refreshInterval) {
    clearInterval(refreshInterval)
  }
})
</script>

<template>
  <div class="max-w-2xl mx-auto">
    <!-- Loading State -->
    <div v-if="sessionStore.loading && !sessionStore.session" class="text-center py-16">
      <div class="inline-flex items-center justify-center w-20 h-20 rounded-full bg-[var(--color-blush)] mb-6 animate-pulse-soft">
        <span class="text-4xl">💫</span>
      </div>
      <p class="text-[var(--color-warm-gray-light)] font-medium">Loading your session...</p>
    </div>

    <!-- Waiting for Partner -->
    <div v-else-if="sessionStore.isWaitingForPartner" class="space-y-6">
      <!-- Preferences Card - Show first if not completed -->
      <div v-if="!myPrefsCompleted" class="card-elevated p-8 md:p-10 text-center animate-slide-up">
        <div class="relative inline-block mb-6">
          <div class="w-24 h-24 rounded-full bg-gradient-to-br from-[var(--color-mint)] to-[#98D9C2] flex items-center justify-center">
            <span class="text-5xl">📝</span>
          </div>
        </div>

        <h1 class="font-display text-3xl font-semibold text-[var(--color-warm-gray)] mb-3">
          Set Your Preferences
        </h1>
        <p class="text-[var(--color-warm-gray-light)] mb-6 max-w-sm mx-auto">
          While waiting for your partner, tell us what name styles appeal to you. This helps us show you better matches!
        </p>

        <RouterLink
          to="/preferences"
          class="btn-primary w-full max-w-sm mx-auto text-center inline-block mb-4"
        >
          <span>Set Your Preferences</span>
        </RouterLink>

        <p class="text-xs text-[var(--color-warm-gray-light)]">
          Takes about 2 minutes
        </p>
      </div>

      <!-- Partner Invite Card -->
      <div class="card-elevated p-8 md:p-10 text-center animate-slide-up" :class="{ 'stagger-2': !myPrefsCompleted }">
        <!-- Floating hearts animation -->
        <div class="relative inline-block mb-6">
          <div class="w-24 h-24 rounded-full bg-gradient-to-br from-[var(--color-peach-light)] to-[var(--color-blush)] flex items-center justify-center animate-float">
            <span class="text-5xl">💑</span>
          </div>
          <div class="absolute -top-2 -right-2 w-8 h-8 rounded-full bg-[var(--color-coral)] flex items-center justify-center animate-pulse-soft">
            <span class="text-sm">✨</span>
          </div>
        </div>

        <h1 class="font-display text-3xl font-semibold text-[var(--color-warm-gray)] mb-3">
          Invite Your Partner
        </h1>
        <p class="text-[var(--color-warm-gray-light)] mb-8 max-w-sm mx-auto">
          Share the code or link below so you can discover baby names together!
        </p>

        <!-- Join Code Display -->
        <div class="bg-[var(--color-cream)] rounded-2xl p-6 mb-6">
          <p class="text-sm font-medium text-[var(--color-warm-gray-light)] mb-3 uppercase tracking-wide">
            Your Session Code
          </p>
          <div class="flex items-center justify-center gap-4">
            <span class="join-code">
              {{ sessionStore.session?.joinCode }}
            </span>
            <button
              @click="copyCode"
              class="copy-btn"
              :class="{ copied: copied === 'code' }"
            >
              {{ copied === 'code' ? '✓ Copied!' : 'Copy' }}
            </button>
          </div>
        </div>

        <!-- Shareable Link -->
        <div class="border-t-2 border-[var(--color-cream-dark)] pt-6">
          <p class="text-sm font-medium text-[var(--color-warm-gray-light)] mb-3">
            Or share this link directly
          </p>
          <div class="link-container">
            <input
              :value="sessionStore.shareableLink"
              readonly
              class="truncate"
            />
            <button
              @click="copyLink"
              class="btn-primary px-5 py-2.5 text-sm whitespace-nowrap"
            >
              <span>{{ copied === 'link' ? '✓ Copied!' : 'Copy Link' }}</span>
            </button>
          </div>
        </div>

        <!-- Waiting indicator -->
        <div class="mt-8 flex items-center justify-center gap-2 text-[var(--color-warm-gray-light)]">
          <div class="flex gap-1">
            <span class="w-2 h-2 rounded-full bg-[var(--color-peach)] animate-pulse" style="animation-delay: 0s;"></span>
            <span class="w-2 h-2 rounded-full bg-[var(--color-peach)] animate-pulse" style="animation-delay: 0.2s;"></span>
            <span class="w-2 h-2 rounded-full bg-[var(--color-peach)] animate-pulse" style="animation-delay: 0.4s;"></span>
          </div>
          <span class="text-sm">Listening for your partner...</span>
        </div>
      </div>

      <!-- Session Info Card -->
      <div class="card p-6 animate-slide-up" :class="myPrefsCompleted ? 'stagger-2' : 'stagger-3'" style="animation-fill-mode: forwards; opacity: 0;">
        <div class="flex items-center gap-4">
          <div class="w-12 h-12 rounded-xl bg-[var(--color-blush)] flex items-center justify-center text-2xl">
            {{ genderIcon }}
          </div>
          <div class="flex-1">
            <h3 class="font-display font-semibold text-[var(--color-warm-gray)]">Session Details</h3>
            <p class="text-sm text-[var(--color-warm-gray-light)]">
              Browsing <strong>{{ genderLabel }}</strong>
            </p>
          </div>
          <div v-if="myPrefsCompleted" class="flex items-center gap-2 text-green-600 text-sm">
            <span>✓</span>
            <span>Preferences set</span>
          </div>
        </div>
      </div>
    </div>

    <!-- Active Session - Partner Connected! -->
    <div v-else-if="sessionStore.isActive" class="space-y-6">
      <div class="card-elevated p-8 md:p-10 text-center animate-bounce-in">
        <!-- Celebration header -->
        <div class="relative inline-block mb-6">
          <div class="w-24 h-24 rounded-full bg-gradient-to-br from-[var(--color-mint)] to-[#98D9C2] flex items-center justify-center">
            <span class="text-5xl">🎉</span>
          </div>
          <!-- Confetti decorations -->
          <span class="absolute -top-4 -left-4 text-2xl animate-bounce" style="animation-delay: 0.1s;">🎊</span>
          <span class="absolute -top-2 -right-6 text-xl animate-bounce" style="animation-delay: 0.3s;">✨</span>
          <span class="absolute -bottom-2 -left-6 text-xl animate-bounce" style="animation-delay: 0.2s;">💫</span>
        </div>

        <h1 class="font-display text-3xl font-semibold text-[var(--color-warm-gray)] mb-3">
          You're Connected!
        </h1>
        <p class="text-[var(--color-warm-gray-light)] mb-6 max-w-sm mx-auto">
          You and <strong class="text-[var(--color-coral)]">{{ partnerName }}</strong> are ready to find the perfect name together.
        </p>

        <!-- Session badge -->
        <div class="inline-flex items-center gap-3 bg-[var(--color-blush)] px-5 py-3 rounded-full mb-6">
          <span class="text-xl">{{ genderIcon }}</span>
          <span class="font-medium text-[var(--color-coral)]">{{ genderLabel }}</span>
        </div>

        <!-- Preference Status Section -->
        <div class="bg-[var(--color-cream)] rounded-2xl p-5 mb-6 text-left">
          <h3 class="font-semibold text-[var(--color-warm-gray)] mb-3 text-center">Preference Setup</h3>
          <div class="space-y-3">
            <!-- My preferences -->
            <div class="flex items-center gap-3">
              <span class="text-xl">{{ myPrefsCompleted ? '✅' : '⏳' }}</span>
              <span class="flex-1 text-sm" :class="myPrefsCompleted ? 'text-green-700' : 'text-[var(--color-warm-gray-light)]'">
                {{ myPrefsCompleted ? 'Your preferences are set' : 'You haven\'t set your preferences yet' }}
              </span>
              <RouterLink
                v-if="!myPrefsCompleted"
                to="/preferences"
                class="text-sm font-semibold text-[var(--color-coral)] hover:underline"
              >
                Set Now →
              </RouterLink>
            </div>
            <!-- Partner preferences -->
            <div class="flex items-center gap-3">
              <span class="text-xl">{{ partnerPrefsCompleted ? '✅' : '⏳' }}</span>
              <span class="flex-1 text-sm" :class="partnerPrefsCompleted ? 'text-green-700' : 'text-[var(--color-warm-gray-light)]'">
                {{ partnerPrefsCompleted ? `${partnerName}'s preferences are set` : `Waiting for ${partnerName}` }}
              </span>
            </div>
          </div>
        </div>

        <!-- Start Button - only enabled when both have completed -->
        <RouterLink
          v-if="canStartVoting"
          to="/swipe"
          class="btn-primary w-full max-w-sm mx-auto text-center inline-block"
        >
          <span>Start Swiping Names →</span>
        </RouterLink>

        <!-- Set Preferences Button when not complete -->
        <RouterLink
          v-else-if="!myPrefsCompleted"
          to="/preferences"
          class="btn-primary w-full max-w-sm mx-auto text-center inline-block"
        >
          <span>Set Your Preferences →</span>
        </RouterLink>

        <!-- Waiting message when user is done but partner isn't -->
        <div v-else class="text-center">
          <p class="text-[var(--color-warm-gray-light)] mb-4">
            Waiting for {{ partnerName }} to complete their preferences...
          </p>
          <RouterLink
            to="/preferences"
            class="text-sm text-[var(--color-coral)] hover:underline"
          >
            Review your preferences
          </RouterLink>
        </div>
      </div>

      <!-- Partner Card -->
      <div class="card p-6 animate-slide-up stagger-2" style="animation-fill-mode: forwards; opacity: 0;">
        <h3 class="font-display font-semibold text-[var(--color-warm-gray)] mb-4">Your Partner</h3>
        <div class="flex items-center gap-4">
          <div class="partner-avatar">
            💝
          </div>
          <div class="flex-1">
            <p class="font-semibold text-[var(--color-warm-gray)]">{{ partnerName }}</p>
            <p class="text-sm text-[var(--color-warm-gray-light)]">
              Joined {{ new Date(sessionStore.session?.linkedAt || '').toLocaleDateString('en-US', {
                month: 'short',
                day: 'numeric',
                year: 'numeric'
              }) }}
            </p>
          </div>
          <div class="status-badge active">
            <span class="w-2 h-2 rounded-full bg-green-500"></span>
            Active
          </div>
        </div>
      </div>
    </div>

    <!-- No Session -->
    <div v-else class="card-elevated p-8 md:p-10 text-center animate-slide-up">
      <div class="inline-flex items-center justify-center w-20 h-20 rounded-full bg-[var(--color-blush)] mb-6">
        <span class="text-4xl">👋</span>
      </div>
      <h1 class="font-display text-3xl font-semibold text-[var(--color-warm-gray)] mb-3">
        No Active Session
      </h1>
      <p class="text-[var(--color-warm-gray-light)] mb-8 max-w-sm mx-auto">
        Create a new session or join your partner to start discovering names together.
      </p>
      <div class="flex flex-col sm:flex-row gap-4 justify-center">
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
