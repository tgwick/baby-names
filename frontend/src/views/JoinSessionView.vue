<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useSessionStore } from '@/stores/session'

const router = useRouter()
const sessionStore = useSessionStore()

const joinCode = ref('')
const loading = ref(false)
const error = ref('')

async function handleJoin() {
  if (joinCode.value.length !== 6) {
    error.value = 'Please enter a 6-character code'
    return
  }

  error.value = ''
  loading.value = true

  try {
    await sessionStore.joinByCode(joinCode.value)
    router.push('/session')
  } catch (e: unknown) {
    const err = e as { response?: { data?: { errors?: string[] } } }
    error.value = err.response?.data?.errors?.[0] || 'Failed to join session. Please check the code and try again.'
  } finally {
    loading.value = false
  }
}

function formatCode(event: Event) {
  const input = event.target as HTMLInputElement
  joinCode.value = input.value.toUpperCase().replace(/[^A-Z0-9]/g, '').slice(0, 6)
}
</script>

<template>
  <div class="max-w-md mx-auto">
    <div class="bg-white p-8 rounded-xl shadow-sm">
      <h1 class="text-2xl font-bold text-center mb-2">Join a Session</h1>
      <p class="text-gray-600 text-center mb-6">
        Enter the 6-character code from your partner
      </p>

      <form @submit.prevent="handleJoin" class="space-y-4">
        <div v-if="error" class="p-3 bg-red-50 text-red-700 rounded-lg text-sm">
          {{ error }}
        </div>

        <div>
          <input
            :value="joinCode"
            @input="formatCode"
            type="text"
            maxlength="6"
            placeholder="XXXXXX"
            class="w-full px-4 py-4 text-center text-2xl font-mono tracking-widest border border-gray-300 rounded-lg focus:ring-2 focus:ring-rose-500 focus:border-transparent uppercase"
          />
        </div>

        <button
          type="submit"
          :disabled="loading || joinCode.length !== 6"
          class="w-full py-3 text-white bg-rose-500 rounded-lg hover:bg-rose-600 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
        >
          {{ loading ? 'Joining...' : 'Join Session' }}
        </button>
      </form>

      <RouterLink
        to="/dashboard"
        class="block mt-4 text-center text-gray-600 hover:text-gray-800"
      >
        Cancel
      </RouterLink>
    </div>
  </div>
</template>
