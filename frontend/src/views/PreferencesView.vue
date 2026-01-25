<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useFiltersStore } from '@/stores/filters'
import { useSessionStore } from '@/stores/session'
import FilterQuestionnaire from '@/components/FilterQuestionnaire.vue'

const router = useRouter()
const filtersStore = useFiltersStore()
const sessionStore = useSessionStore()

const submitting = ref(false)
const submitted = ref(false)
const error = ref('')

const stepTitle = computed(() => {
  if (submitted.value) return 'Setup Complete!'
  return 'Filter Your Names'
})

const stepSubtitle = computed(() => {
  if (submitted.value) return 'Here\'s what you told us about your preferences'
  return 'Let\'s narrow down the name pool to your taste'
})

const stepEmoji = computed(() => {
  if (submitted.value) return '🎉'
  return '🔍'
})

onMounted(async () => {
  // Fetch session if not loaded
  if (!sessionStore.session) {
    await sessionStore.fetchCurrentSession()
  }

  // Redirect if no active session
  if (!sessionStore.session) {
    router.push('/dashboard')
    return
  }

  // Fetch filter questions and status
  await Promise.all([
    filtersStore.fetchQuestions(),
    filtersStore.fetchStatus(),
  ])

  // Check if already completed
  const isInitiator = sessionStore.session.isInitiator
  const filtersCompleted = isInitiator
    ? filtersStore.status?.initiatorCompleted
    : filtersStore.status?.partnerCompleted

  if (filtersCompleted) {
    // Already completed, show summary
    submitted.value = true
    await filtersStore.fetchUserFilters()
  }
})

async function handleFiltersComplete() {
  submitting.value = true
  error.value = ''

  try {
    await filtersStore.submitFilters()
    submitted.value = true

    // Refresh session to get updated status
    await sessionStore.refreshSession()
    await filtersStore.fetchUserFilters()
  } catch (e: unknown) {
    const err = e as { response?: { data?: { errors?: string[] } } }
    error.value = err.response?.data?.errors?.[0] || 'Failed to save filters. Please try again.'
  } finally {
    submitting.value = false
  }
}

function handleContinue() {
  router.push('/session')
}

function handleStartOver() {
  filtersStore.resetQuestionnaire()
  submitted.value = false
}

function getNameStyleLabel(style: number): string {
  switch (style) {
    case 1: return 'Trendy names'
    case 2: return 'Classic names'
    case 3: return 'Unique names'
    default: return 'All name styles'
  }
}

function getSyllableLabel(min: number | null, max: number | null): string {
  if (min === null && max === null) return 'Any length'
  if (max !== null && max <= 2) return 'Short names (1-2 syllables)'
  if (min !== null && min >= 2 && max !== null && max <= 3) return 'Medium length (2-3 syllables)'
  if (min !== null && min >= 3) return 'Long names (3+ syllables)'
  return 'Custom length'
}
</script>

<template>
  <div class="max-w-lg mx-auto">
    <div class="card-elevated p-6 md:p-8 animate-slide-up">
      <!-- Header -->
      <div class="text-center mb-6">
        <div
          class="inline-flex items-center justify-center w-14 h-14 rounded-full mb-3"
          :class="submitted ? 'bg-[var(--color-blush)]' : 'bg-[var(--color-sage-light,#d4e5d4)]'"
        >
          <span class="text-2xl">{{ stepEmoji }}</span>
        </div>
        <h1 class="font-display text-2xl font-semibold text-[var(--color-warm-gray)] mb-1">
          {{ stepTitle }}
        </h1>
        <p class="text-sm text-[var(--color-warm-gray-light)]">
          {{ stepSubtitle }}
        </p>
      </div>

      <!-- Error Message -->
      <div v-if="error" class="error-message mb-6 animate-slide-up">
        {{ error }}
      </div>

      <!-- Submitting state -->
      <div v-if="submitting" class="text-center py-12">
        <div
          class="animate-spin w-10 h-10 border-4 border-[var(--color-sage)] border-t-transparent rounded-full mx-auto mb-4"
        />
        <p class="text-[var(--color-warm-gray-light)]">Saving your filters...</p>
      </div>

      <!-- Submitted summary -->
      <div v-else-if="submitted" class="space-y-6">
        <!-- Filters summary -->
        <div v-if="filtersStore.userFilters" class="space-y-3">
          <h3 class="font-semibold text-[var(--color-warm-gray)]">Your Filters</h3>
          <div class="flex flex-wrap gap-2">
            <span class="inline-flex items-center px-3 py-1.5 rounded-full text-sm font-medium bg-[var(--color-sage-light,#d4e5d4)] text-[var(--color-sage-dark,#5a8a5a)]">
              {{ getNameStyleLabel(filtersStore.userFilters.nameStyle) }}
            </span>
            <span class="inline-flex items-center px-3 py-1.5 rounded-full text-sm font-medium bg-[var(--color-sage-light,#d4e5d4)] text-[var(--color-sage-dark,#5a8a5a)]">
              {{ getSyllableLabel(filtersStore.userFilters.minSyllables, filtersStore.userFilters.maxSyllables) }}
            </span>
          </div>
        </div>

        <!-- Partner status -->
        <div class="p-4 rounded-xl bg-[var(--color-cream)]">
          <div v-if="filtersStore.status?.bothCompleted" class="text-center">
            <span class="text-2xl mb-2 block">🎉</span>
            <p class="font-semibold text-[var(--color-warm-gray)]">You're both ready!</p>
            <p class="text-sm text-[var(--color-warm-gray-light)]">Time to start discovering names together</p>
          </div>
          <div v-else class="text-center">
            <span class="text-2xl mb-2 block">⏳</span>
            <p class="font-semibold text-[var(--color-warm-gray)]">Waiting for your partner</p>
            <p class="text-sm text-[var(--color-warm-gray-light)]">They need to complete their setup too</p>
          </div>
        </div>

        <!-- Actions -->
        <div class="space-y-3 pt-2">
          <button
            v-if="filtersStore.status?.bothCompleted"
            @click="handleContinue"
            class="btn-primary w-full text-center"
          >
            <span>Start Swiping Names</span>
          </button>
          <button
            v-else
            @click="handleContinue"
            class="btn-primary w-full text-center"
          >
            <span>Back to Session</span>
          </button>
          <button
            @click="handleStartOver"
            class="w-full py-3 text-center text-[var(--color-warm-gray-light)] hover:text-[var(--color-coral)] transition-colors"
          >
            Update My Filters
          </button>
        </div>
      </div>

      <!-- Filter Questionnaire -->
      <FilterQuestionnaire
        v-else
        @complete="handleFiltersComplete"
      />

      <!-- Back link -->
      <RouterLink
        v-if="!submitted && !submitting"
        to="/session"
        class="block mt-6 text-center text-sm text-[var(--color-warm-gray-light)] hover:text-[var(--color-coral)] transition-colors"
      >
        ← Skip for now
      </RouterLink>
    </div>
  </div>
</template>
