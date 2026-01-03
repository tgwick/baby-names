<script setup lang="ts">
import { onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useSessionStore } from '@/stores/session'

const router = useRouter()
const authStore = useAuthStore()
const sessionStore = useSessionStore()

onMounted(async () => {
  await sessionStore.fetchCurrentSession()

  // Check for pending join link after login
  const pendingLink = localStorage.getItem('pendingJoinLink')
  if (pendingLink) {
    localStorage.removeItem('pendingJoinLink')
    router.push(`/join/${pendingLink}`)
  }
})

function goToSession() {
  router.push('/session')
}
</script>

<template>
  <div class="max-w-4xl mx-auto">
    <h1 class="text-3xl font-bold mb-8">
      Welcome{{ authStore.user?.displayName ? `, ${authStore.user.displayName}` : '' }}!
    </h1>

    <!-- Active Session Banner -->
    <div
      v-if="sessionStore.hasSession"
      class="mb-6 p-4 bg-rose-50 border border-rose-200 rounded-xl"
    >
      <div class="flex items-center justify-between">
        <div class="flex items-center gap-3">
          <span class="text-2xl">
            {{ sessionStore.isWaitingForPartner ? '⏳' : '💑' }}
          </span>
          <div>
            <p class="font-medium text-rose-700">
              {{ sessionStore.isWaitingForPartner ? 'Waiting for partner to join' : 'Session active!' }}
            </p>
            <p class="text-sm text-rose-600">
              {{ sessionStore.isWaitingForPartner
                ? `Share code: ${sessionStore.session?.joinCode}`
                : `With ${sessionStore.session?.isInitiator ? sessionStore.session?.partnerDisplayName : sessionStore.session?.initiatorDisplayName}`
              }}
            </p>
          </div>
        </div>
        <button
          @click="goToSession"
          class="px-4 py-2 text-white bg-rose-500 rounded-lg hover:bg-rose-600 transition-colors"
        >
          View Session
        </button>
      </div>
    </div>

    <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
      <div class="bg-white p-6 rounded-xl shadow-sm">
        <h2 class="text-xl font-semibold mb-4">Start a New Session</h2>
        <p class="text-gray-600 mb-4">
          Create a new session to start swiping through baby names.
          Your partner can join using a unique code.
        </p>
        <RouterLink
          to="/session/create"
          :class="[
            'block w-full py-3 text-center rounded-lg transition-colors',
            sessionStore.hasSession
              ? 'text-gray-400 bg-gray-100 cursor-not-allowed pointer-events-none'
              : 'text-white bg-rose-500 hover:bg-rose-600'
          ]"
        >
          {{ sessionStore.hasSession ? 'Session Already Active' : 'Create Session' }}
        </RouterLink>
      </div>

      <div class="bg-white p-6 rounded-xl shadow-sm">
        <h2 class="text-xl font-semibold mb-4">Join a Session</h2>
        <p class="text-gray-600 mb-4">
          Have a code from your partner? Enter it here to join their session
          and start finding names together.
        </p>
        <RouterLink
          to="/session/join"
          :class="[
            'block w-full py-3 text-center rounded-lg transition-colors',
            sessionStore.hasSession
              ? 'text-gray-400 bg-gray-100 cursor-not-allowed pointer-events-none'
              : 'text-gray-700 bg-gray-100 hover:bg-gray-200'
          ]"
        >
          {{ sessionStore.hasSession ? 'Session Already Active' : 'Join Session' }}
        </RouterLink>
      </div>
    </div>

    <div class="mt-8 bg-white p-6 rounded-xl shadow-sm">
      <h2 class="text-xl font-semibold mb-4">Your Matches</h2>
      <p class="text-gray-600">
        {{ sessionStore.hasSession && sessionStore.isActive
          ? 'No matches yet. Start swiping to find names you both love!'
          : 'No matches yet. Start a session to begin discovering names!'
        }}
      </p>
    </div>
  </div>
</template>
