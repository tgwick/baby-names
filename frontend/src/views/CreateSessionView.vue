<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useSessionStore } from '@/stores/session'
import { Gender } from '@/types/session'

const router = useRouter()
const sessionStore = useSessionStore()

const selectedGender = ref<Gender>(Gender.Neutral)
const loading = ref(false)
const error = ref('')

const genderOptions = [
  { value: Gender.Male, label: 'Boy Names', icon: '👦' },
  { value: Gender.Female, label: 'Girl Names', icon: '👧' },
  { value: Gender.Neutral, label: 'All Names', icon: '👶' },
]

async function handleCreate() {
  error.value = ''
  loading.value = true

  try {
    await sessionStore.createSession(selectedGender.value)
    router.push('/session')
  } catch (e: unknown) {
    const err = e as { response?: { data?: { errors?: string[] } } }
    error.value = err.response?.data?.errors?.[0] || 'Failed to create session. Please try again.'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="max-w-md mx-auto">
    <div class="bg-white p-8 rounded-xl shadow-sm">
      <h1 class="text-2xl font-bold text-center mb-2">Create a Session</h1>
      <p class="text-gray-600 text-center mb-6">
        Choose what type of names you'd like to browse
      </p>

      <div v-if="error" class="mb-4 p-3 bg-red-50 text-red-700 rounded-lg text-sm">
        {{ error }}
      </div>

      <div class="space-y-3 mb-6">
        <button
          v-for="option in genderOptions"
          :key="option.value"
          type="button"
          @click="selectedGender = option.value"
          :class="[
            'w-full p-4 rounded-lg border-2 text-left flex items-center gap-4 transition-all',
            selectedGender === option.value
              ? 'border-rose-500 bg-rose-50'
              : 'border-gray-200 hover:border-gray-300',
          ]"
        >
          <span class="text-3xl">{{ option.icon }}</span>
          <span class="font-medium">{{ option.label }}</span>
        </button>
      </div>

      <button
        @click="handleCreate"
        :disabled="loading"
        class="w-full py-3 text-white bg-rose-500 rounded-lg hover:bg-rose-600 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
      >
        {{ loading ? 'Creating...' : 'Create Session' }}
      </button>

      <RouterLink
        to="/dashboard"
        class="block mt-4 text-center text-gray-600 hover:text-gray-800"
      >
        Cancel
      </RouterLink>
    </div>
  </div>
</template>
