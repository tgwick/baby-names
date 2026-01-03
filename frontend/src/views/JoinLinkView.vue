<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useSessionStore } from '@/stores/session'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const route = useRoute()
const sessionStore = useSessionStore()
const authStore = useAuthStore()

const loading = ref(true)
const error = ref('')

onMounted(async () => {
  const partnerLink = route.params.partnerLink as string

  if (!partnerLink) {
    error.value = 'Invalid link'
    loading.value = false
    return
  }

  if (!authStore.isAuthenticated) {
    localStorage.setItem('pendingJoinLink', partnerLink)
    router.push('/login')
    return
  }

  try {
    await sessionStore.joinByLink(partnerLink)
    router.push('/session')
  } catch (e: unknown) {
    const err = e as { response?: { data?: { errors?: string[] } } }
    error.value = err.response?.data?.errors?.[0] || 'Failed to join session. The link may be invalid or expired.'
    loading.value = false
  }
})
</script>

<template>
  <div class="max-w-lg mx-auto">
    <div class="card-elevated p-8 md:p-10 text-center animate-slide-up">
      <!-- Loading State -->
      <template v-if="loading">
        <div class="relative inline-block mb-6">
          <div class="w-24 h-24 rounded-full bg-gradient-to-br from-[var(--color-peach-light)] to-[var(--color-blush)] flex items-center justify-center animate-pulse-soft">
            <span class="text-5xl">💑</span>
          </div>
          <div class="absolute -top-2 -right-2 w-8 h-8 rounded-full bg-[var(--color-coral)] flex items-center justify-center animate-bounce">
            <span class="text-sm">✨</span>
          </div>
        </div>
        <h1 class="font-display text-2xl font-semibold text-[var(--color-warm-gray)] mb-3">
          Connecting You...
        </h1>
        <p class="text-[var(--color-warm-gray-light)]">
          Please wait while we link you with your partner.
        </p>
        <!-- Loading dots -->
        <div class="mt-6 flex items-center justify-center gap-1">
          <span class="w-2 h-2 rounded-full bg-[var(--color-peach)] animate-pulse" style="animation-delay: 0s;"></span>
          <span class="w-2 h-2 rounded-full bg-[var(--color-peach)] animate-pulse" style="animation-delay: 0.2s;"></span>
          <span class="w-2 h-2 rounded-full bg-[var(--color-peach)] animate-pulse" style="animation-delay: 0.4s;"></span>
        </div>
      </template>

      <!-- Error State -->
      <template v-else-if="error">
        <div class="inline-flex items-center justify-center w-20 h-20 rounded-full bg-[#FFEBEE] mb-6">
          <span class="text-4xl">😕</span>
        </div>
        <h1 class="font-display text-2xl font-semibold text-[var(--color-warm-gray)] mb-3">
          Unable to Join
        </h1>
        <p class="text-[var(--color-warm-gray-light)] mb-8">
          {{ error }}
        </p>
        <div class="flex flex-col sm:flex-row gap-4 justify-center">
          <RouterLink to="/session/join" class="btn-primary">
            <span>Try with Code</span>
          </RouterLink>
          <RouterLink to="/dashboard" class="btn-secondary">
            Go to Dashboard
          </RouterLink>
        </div>
      </template>
    </div>
  </div>
</template>
