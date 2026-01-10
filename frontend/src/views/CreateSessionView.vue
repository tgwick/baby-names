<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useSessionStore } from '@/stores/session'
import { Gender } from '@/types/session'

const router = useRouter()
const sessionStore = useSessionStore()

const selectedGender = ref<Gender | null>(null)
const loading = ref(false)
const error = ref('')

const genderOptions = [
  {
    value: Gender.Male,
    label: 'Boy Names',
    icon: '👦',
    description: 'Browse masculine names',
    color: 'sky',
  },
  {
    value: Gender.Female,
    label: 'Girl Names',
    icon: '👧',
    description: 'Browse feminine names',
    color: 'lavender',
  },
  {
    value: Gender.Neutral,
    label: 'All Names',
    icon: '👶',
    description: 'Browse all names together',
    color: 'mint',
  },
]

async function handleCreate() {
  if (selectedGender.value === null) {
    error.value = 'Please select a name category'
    return
  }

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
  <div class="max-w-lg mx-auto">
    <div class="card-elevated p-8 md:p-10 animate-slide-up">
      <!-- Header -->
      <div class="text-center mb-8">
        <div class="inline-flex items-center justify-center w-16 h-16 rounded-full bg-[var(--color-blush)] mb-4 animate-bounce-in">
          <span class="text-3xl">🪺</span>
        </div>
        <h1 class="font-display text-3xl font-semibold text-[var(--color-warm-gray)] mb-2">
          Build Your Nest
        </h1>
        <p class="text-[var(--color-warm-gray-light)]">
          What kind of names would you like to hatch?
        </p>
      </div>

      <!-- Error Message -->
      <div v-if="error" class="error-message mb-6 animate-slide-up">
        {{ error }}
      </div>

      <!-- Gender Selection -->
      <div class="space-y-4 mb-8">
        <button
          v-for="(option, index) in genderOptions"
          :key="option.value"
          type="button"
          @click="selectedGender = option.value"
          class="gender-card w-full text-left opacity-0 animate-slide-up"
          :class="[
            selectedGender === option.value ? 'selected' : '',
            `stagger-${index + 1}`,
          ]"
          :style="{ animationFillMode: 'forwards' }"
        >
          <div class="flex items-center gap-4">
            <span class="icon">{{ option.icon }}</span>
            <div class="flex-1">
              <span class="font-display text-lg font-semibold text-[var(--color-warm-gray)] block">
                {{ option.label }}
              </span>
              <span class="text-sm text-[var(--color-warm-gray-light)]">
                {{ option.description }}
              </span>
            </div>
            <div
              class="w-6 h-6 rounded-full border-2 flex items-center justify-center transition-all"
              :class="
                selectedGender === option.value
                  ? 'border-[var(--color-coral)] bg-[var(--color-coral)]'
                  : 'border-[var(--color-peach-light)]'
              "
            >
              <svg
                v-if="selectedGender === option.value"
                class="w-4 h-4 text-white"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="3" d="M5 13l4 4L19 7" />
              </svg>
            </div>
          </div>
        </button>
      </div>

      <!-- Create Button -->
      <button
        @click="handleCreate"
        :disabled="loading || selectedGender === null"
        class="btn-primary w-full text-center"
      >
        <span>{{ loading ? 'Building your nest...' : 'Build Nest' }}</span>
      </button>

      <!-- Cancel Link -->
      <RouterLink
        to="/dashboard"
        class="block mt-6 text-center text-[var(--color-warm-gray-light)] hover:text-[var(--color-coral)] transition-colors"
      >
        ← Back to Dashboard
      </RouterLink>
    </div>
  </div>
</template>
