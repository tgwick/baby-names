<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { usePreferencesStore } from '@/stores/preferences'
import { useSessionStore } from '@/stores/session'
import PreferenceQuestionnaire from '@/components/PreferenceQuestionnaire.vue'

const router = useRouter()
const preferencesStore = usePreferencesStore()
const sessionStore = useSessionStore()

const submitting = ref(false)
const submitted = ref(false)
const error = ref('')

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

  // Fetch questions
  await preferencesStore.fetchQuestions()
  await preferencesStore.fetchStatus()

  // If already completed, show summary
  const isInitiator = sessionStore.session.isInitiator
  const alreadyCompleted = isInitiator
    ? sessionStore.session.initiatorPrefsCompleted
    : sessionStore.session.partnerPrefsCompleted

  if (alreadyCompleted) {
    submitted.value = true
    await preferencesStore.fetchUserPreferences()
  }
})

async function handleComplete() {
  submitting.value = true
  error.value = ''

  try {
    await preferencesStore.submitPreferences()
    submitted.value = true

    // Refresh session to get updated status
    await sessionStore.refreshSession()
    await preferencesStore.fetchUserPreferences()
  } catch (e: unknown) {
    const err = e as { response?: { data?: { errors?: string[] } } }
    error.value = err.response?.data?.errors?.[0] || 'Failed to save preferences. Please try again.'
  } finally {
    submitting.value = false
  }
}

function handleContinue() {
  router.push('/session')
}

function handleStartOver() {
  preferencesStore.resetQuestionnaire()
  submitted.value = false
}
</script>

<template>
  <div class="max-w-lg mx-auto">
    <div class="card-elevated p-6 md:p-8 animate-slide-up">
      <!-- Header -->
      <div class="text-center mb-6">
        <div class="inline-flex items-center justify-center w-14 h-14 rounded-full bg-[var(--color-blush)] mb-3">
          <span class="text-2xl">💝</span>
        </div>
        <h1 class="font-display text-2xl font-semibold text-[var(--color-warm-gray)] mb-1">
          {{ submitted ? 'Preferences Saved!' : 'Your Preferences' }}
        </h1>
        <p class="text-sm text-[var(--color-warm-gray-light)]">
          {{ submitted
            ? 'Here\'s what you told us about your name preferences'
            : 'Help us find names you\'ll love' }}
        </p>
      </div>

      <!-- Error Message -->
      <div v-if="error" class="error-message mb-6 animate-slide-up">
        {{ error }}
      </div>

      <!-- Submitting state -->
      <div v-if="submitting" class="text-center py-12">
        <div class="animate-spin w-10 h-10 border-4 border-[var(--color-peach)] border-t-transparent rounded-full mx-auto mb-4" />
        <p class="text-[var(--color-warm-gray-light)]">Saving your preferences...</p>
      </div>

      <!-- Submitted summary -->
      <div v-else-if="submitted" class="space-y-6">
        <!-- Preferences summary -->
        <div v-if="preferencesStore.userPreferences.length > 0" class="space-y-3">
          <h3 class="font-semibold text-[var(--color-warm-gray)]">Your Preferences:</h3>
          <div class="flex flex-wrap gap-2">
            <span
              v-for="pref in preferencesStore.userPreferences"
              :key="pref.id"
              class="inline-flex items-center px-3 py-1.5 rounded-full text-sm font-medium"
              :class="{
                'bg-green-100 text-green-800': pref.level > 0,
                'bg-gray-100 text-gray-600': pref.level === 0,
                'bg-red-100 text-red-800': pref.level < 0,
              }"
            >
              {{ pref.level > 0 ? '❤️' : pref.level < 0 ? '👎' : '➖' }}
              {{ pref.categoryName }}
            </span>
          </div>
        </div>

        <div v-else class="text-center py-4 text-[var(--color-warm-gray-light)]">
          No specific preferences set - you're open to all names!
        </div>

        <!-- Partner status -->
        <div class="p-4 rounded-xl bg-[var(--color-cream)]">
          <div v-if="preferencesStore.status?.bothCompleted" class="text-center">
            <span class="text-2xl mb-2 block">🎉</span>
            <p class="font-semibold text-[var(--color-warm-gray)]">You're both ready!</p>
            <p class="text-sm text-[var(--color-warm-gray-light)]">Time to start discovering names together</p>
          </div>
          <div v-else class="text-center">
            <span class="text-2xl mb-2 block">⏳</span>
            <p class="font-semibold text-[var(--color-warm-gray)]">Waiting for your partner</p>
            <p class="text-sm text-[var(--color-warm-gray-light)]">They need to complete their preferences too</p>
          </div>
        </div>

        <!-- Actions -->
        <div class="space-y-3 pt-2">
          <button
            v-if="preferencesStore.status?.bothCompleted"
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
            Update My Preferences
          </button>
        </div>
      </div>

      <!-- Questionnaire -->
      <PreferenceQuestionnaire
        v-else
        @complete="handleComplete"
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
