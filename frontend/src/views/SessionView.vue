<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useSessionStore } from '@/stores/session'
import { Gender } from '@/types/session'

const router = useRouter()
const sessionStore = useSessionStore()

const copied = ref(false)
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

const partnerName = computed(() => {
  if (!sessionStore.session) return ''
  return sessionStore.session.isInitiator
    ? sessionStore.session.partnerDisplayName
    : sessionStore.session.initiatorDisplayName
})

async function copyCode() {
  if (!sessionStore.session) return
  await navigator.clipboard.writeText(sessionStore.session.joinCode)
  copied.value = true
  setTimeout(() => (copied.value = false), 2000)
}

async function copyLink() {
  if (!sessionStore.shareableLink) return
  await navigator.clipboard.writeText(sessionStore.shareableLink)
  copied.value = true
  setTimeout(() => (copied.value = false), 2000)
}

onMounted(async () => {
  await sessionStore.fetchCurrentSession()

  if (!sessionStore.hasSession) {
    router.push('/dashboard')
    return
  }

  // Poll for partner joining every 5 seconds while waiting
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
    <div v-if="sessionStore.loading && !sessionStore.session" class="text-center py-12">
      <div class="animate-pulse">
        <div class="text-4xl mb-4">⏳</div>
        <p class="text-gray-600">Loading session...</p>
      </div>
    </div>

    <!-- Waiting for Partner -->
    <div v-else-if="sessionStore.isWaitingForPartner" class="space-y-6">
      <div class="bg-white p-8 rounded-xl shadow-sm text-center">
        <div class="text-5xl mb-4">💑</div>
        <h1 class="text-2xl font-bold mb-2">Waiting for Your Partner</h1>
        <p class="text-gray-600 mb-6">
          Share the code or link below with your partner to get started!
        </p>

        <div class="bg-gray-50 p-6 rounded-lg mb-6">
          <p class="text-sm text-gray-500 mb-2">Join Code</p>
          <div class="flex items-center justify-center gap-4">
            <span class="text-4xl font-mono font-bold tracking-widest">
              {{ sessionStore.session?.joinCode }}
            </span>
            <button
              @click="copyCode"
              class="p-2 text-gray-500 hover:text-gray-700 hover:bg-gray-200 rounded-lg transition-colors"
              :title="copied ? 'Copied!' : 'Copy code'"
            >
              <span v-if="copied">✓</span>
              <span v-else>📋</span>
            </button>
          </div>
        </div>

        <div class="border-t pt-6">
          <p class="text-sm text-gray-500 mb-3">Or share this link</p>
          <div class="flex items-center gap-2 bg-gray-50 p-3 rounded-lg">
            <input
              :value="sessionStore.shareableLink"
              readonly
              class="flex-1 bg-transparent text-sm text-gray-600 outline-none"
            />
            <button
              @click="copyLink"
              class="px-4 py-2 text-sm text-white bg-rose-500 rounded-lg hover:bg-rose-600 transition-colors"
            >
              {{ copied ? 'Copied!' : 'Copy Link' }}
            </button>
          </div>
        </div>
      </div>

      <div class="bg-white p-6 rounded-xl shadow-sm">
        <h2 class="font-semibold mb-2">Session Details</h2>
        <div class="text-gray-600">
          <p><span class="font-medium">Looking for:</span> {{ genderLabel }}</p>
          <p><span class="font-medium">Status:</span> Waiting for partner to join</p>
        </div>
      </div>
    </div>

    <!-- Active Session -->
    <div v-else-if="sessionStore.isActive" class="space-y-6">
      <div class="bg-white p-8 rounded-xl shadow-sm text-center">
        <div class="text-5xl mb-4">🎉</div>
        <h1 class="text-2xl font-bold mb-2">You're Connected!</h1>
        <p class="text-gray-600 mb-4">
          You and <strong>{{ partnerName }}</strong> are ready to start finding baby names together.
        </p>

        <div class="bg-rose-50 p-4 rounded-lg mb-6">
          <p class="text-sm text-rose-700">
            <span class="font-medium">Looking for:</span> {{ genderLabel }}
          </p>
        </div>

        <button
          disabled
          class="w-full py-4 text-white bg-rose-500 rounded-lg hover:bg-rose-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
        >
          Start Swiping (Coming in Phase 5)
        </button>
      </div>

      <div class="bg-white p-6 rounded-xl shadow-sm">
        <h2 class="font-semibold mb-4">Your Partner</h2>
        <div class="flex items-center gap-4">
          <div class="w-12 h-12 bg-rose-100 rounded-full flex items-center justify-center text-2xl">
            👤
          </div>
          <div>
            <p class="font-medium">{{ partnerName }}</p>
            <p class="text-sm text-gray-500">
              Joined {{ new Date(sessionStore.session?.linkedAt || '').toLocaleDateString() }}
            </p>
          </div>
        </div>
      </div>
    </div>

    <!-- No Session -->
    <div v-else class="bg-white p-8 rounded-xl shadow-sm text-center">
      <div class="text-5xl mb-4">👋</div>
      <h1 class="text-2xl font-bold mb-2">No Active Session</h1>
      <p class="text-gray-600 mb-6">
        Create a new session or join your partner's session to get started.
      </p>
      <div class="flex flex-col sm:flex-row gap-4 justify-center">
        <RouterLink
          to="/session/create"
          class="px-6 py-3 text-white bg-rose-500 rounded-lg hover:bg-rose-600 transition-colors"
        >
          Create Session
        </RouterLink>
        <RouterLink
          to="/session/join"
          class="px-6 py-3 text-gray-700 bg-gray-100 rounded-lg hover:bg-gray-200 transition-colors"
        >
          Join Session
        </RouterLink>
      </div>
    </div>
  </div>
</template>
