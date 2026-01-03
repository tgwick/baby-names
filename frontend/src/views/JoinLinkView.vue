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
    // Store the link and redirect to login
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
  <div class="max-w-md mx-auto">
    <div class="bg-white p-8 rounded-xl shadow-sm text-center">
      <template v-if="loading">
        <div class="animate-pulse">
          <div class="text-4xl mb-4">💑</div>
          <h1 class="text-xl font-semibold mb-2">Joining Session...</h1>
          <p class="text-gray-600">Please wait while we connect you with your partner.</p>
        </div>
      </template>

      <template v-else-if="error">
        <div class="text-4xl mb-4">😕</div>
        <h1 class="text-xl font-semibold mb-2 text-red-600">Unable to Join</h1>
        <p class="text-gray-600 mb-6">{{ error }}</p>
        <RouterLink
          to="/dashboard"
          class="inline-block px-6 py-3 text-white bg-rose-500 rounded-lg hover:bg-rose-600 transition-colors"
        >
          Go to Dashboard
        </RouterLink>
      </template>
    </div>
  </div>
</template>
