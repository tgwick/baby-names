<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useSessionStore } from '@/stores/session'
import { Gender } from '@/types/session'
import { extractApiError } from '@/services/api'

const router = useRouter()
const sessionStore = useSessionStore()

const loading = ref(true)
const error = ref<string | null>(null)

const genderIcon = (gender: Gender) => {
  switch (gender) {
    case Gender.Male:
      return '👦'
    case Gender.Female:
      return '👧'
    default:
      return '👶'
  }
}

const sortedMatches = computed(() => {
  return [...sessionStore.matches].sort((a, b) =>
    new Date(b.matchedAt).getTime() - new Date(a.matchedAt).getTime()
  )
})

async function loadData() {
  loading.value = true
  error.value = null

  try {
    await sessionStore.fetchCurrentSession()

    if (!sessionStore.hasSession || !sessionStore.isActive) {
      router.push('/dashboard')
      return
    }

    await sessionStore.fetchMatches()
  } catch (e) {
    error.value = extractApiError(e).message
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  loadData()
})

function formatDate(dateStr: string) {
  return new Date(dateStr).toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
  })
}
</script>

<template>
  <div class="max-w-2xl mx-auto">
    <!-- Header -->
    <div class="flex items-center justify-between mb-6 sm:mb-8 animate-slide-up">
      <button
        @click="router.push('/swipe')"
        class="flex items-center gap-2 text-[var(--color-warm-gray-light)] hover:text-[var(--color-coral)] transition-colors min-h-[44px] min-w-[44px] px-2"
      >
        <span class="text-xl">←</span>
        <span class="font-medium">Keep Swiping</span>
      </button>
    </div>

    <!-- Title -->
    <div class="text-center mb-6 sm:mb-8 animate-slide-up stagger-1" style="animation-fill-mode: forwards; opacity: 0;">
      <div class="inline-flex items-center justify-center w-16 h-16 sm:w-20 sm:h-20 rounded-full bg-[var(--color-blush)] mb-3 sm:mb-4">
        <span class="text-3xl sm:text-4xl">🐣</span>
      </div>
      <h1 class="font-display text-3xl sm:text-4xl font-bold text-[var(--color-warm-gray)]">
        Hatched Names
      </h1>
      <p v-if="!loading && !error" class="text-sm sm:text-base text-[var(--color-warm-gray-light)] mt-2">
        {{ sessionStore.matches.length }} {{ sessionStore.matches.length === 1 ? 'name' : 'names' }} you both love
      </p>
    </div>

    <!-- Loading state -->
    <div v-if="loading" class="text-center py-16 animate-slide-up">
      <div class="inline-flex items-center justify-center w-16 h-16 rounded-full bg-[var(--color-blush)] mb-4 animate-pulse-soft">
        <span class="text-3xl">🥚</span>
      </div>
      <p class="text-[var(--color-warm-gray-light)]">Warming up your names...</p>
    </div>

    <!-- Error state -->
    <div v-else-if="error" class="card-elevated p-6 sm:p-10 text-center animate-bounce-in">
      <div class="w-20 h-20 sm:w-24 sm:h-24 mx-auto rounded-full bg-red-100 flex items-center justify-center mb-4 sm:mb-6">
        <span class="text-4xl sm:text-5xl">😢</span>
      </div>
      <h2 class="font-display text-xl sm:text-2xl font-semibold text-[var(--color-warm-gray)] mb-2 sm:mb-3">
        Couldn't load matches
      </h2>
      <p class="text-sm sm:text-base text-[var(--color-warm-gray-light)] mb-4 sm:mb-6">
        {{ error }}
      </p>
      <button @click="loadData" class="btn-primary">
        <span>Try Again</span>
      </button>
    </div>

    <!-- Matches list -->
    <div v-else-if="sessionStore.matches.length > 0" class="space-y-3 sm:space-y-4">
      <div
        v-for="(match, index) in sortedMatches"
        :key="match.nameId"
        class="card p-4 sm:p-6 animate-slide-up hover:shadow-lg transition-shadow"
        :style="`animation-delay: ${0.1 + index * 0.05}s; animation-fill-mode: forwards; opacity: 0;`"
      >
        <div class="flex items-center gap-3 sm:gap-4">
          <!-- Icon -->
          <div class="w-12 h-12 sm:w-16 sm:h-16 rounded-xl sm:rounded-2xl bg-gradient-to-br from-[var(--color-peach-light)] to-[var(--color-peach)] flex items-center justify-center flex-shrink-0">
            <span class="text-2xl sm:text-3xl">{{ genderIcon(match.gender) }}</span>
          </div>

          <!-- Name info -->
          <div class="flex-1 min-w-0">
            <h3 class="font-display text-xl sm:text-2xl font-semibold text-[var(--color-warm-gray)] truncate">
              {{ match.nameText }}
            </h3>
            <div class="flex flex-wrap items-center gap-1.5 sm:gap-2 mt-1">
              <span v-if="match.origin" class="text-xs sm:text-sm text-[var(--color-warm-gray-light)]">
                {{ match.origin }}
              </span>
              <span class="text-xs text-[var(--color-warm-gray-light)]">
                • {{ formatDate(match.matchedAt) }}
              </span>
            </div>
          </div>

          <!-- Match indicator -->
          <div class="text-2xl sm:text-3xl animate-pulse-soft flex-shrink-0">
            💗
          </div>
        </div>

        <!-- Popularity bar -->
        <div class="mt-3 sm:mt-4 pt-3 sm:pt-4 border-t border-[var(--color-cream-dark)]">
          <div class="flex items-center justify-between text-xs sm:text-sm mb-2">
            <span class="text-[var(--color-warm-gray-light)]">Popularity</span>
            <span class="font-semibold text-[var(--color-coral)]">{{ match.popularityScore }}%</span>
          </div>
          <div class="h-1.5 sm:h-2 bg-[var(--color-cream)] rounded-full overflow-hidden">
            <div
              class="h-full bg-gradient-to-r from-[var(--color-peach)] to-[var(--color-coral)] rounded-full transition-all duration-500"
              :style="`width: ${match.popularityScore}%`"
            />
          </div>
        </div>
      </div>
    </div>

    <!-- Empty state -->
    <div v-else class="card-elevated p-6 sm:p-10 text-center animate-slide-up stagger-2" style="animation-fill-mode: forwards; opacity: 0;">
      <div class="w-20 h-20 sm:w-24 sm:h-24 mx-auto rounded-full bg-[var(--color-cream)] flex items-center justify-center mb-4 sm:mb-6">
        <span class="text-4xl sm:text-5xl">🥚</span>
      </div>
      <h2 class="font-display text-xl sm:text-2xl font-semibold text-[var(--color-warm-gray)] mb-2 sm:mb-3">
        No Hatched Names Yet
      </h2>
      <p class="text-sm sm:text-base text-[var(--color-warm-gray-light)] mb-4 sm:mb-6 max-w-sm mx-auto">
        Keep swiping! When you and your partner both love a name, it'll hatch here.
      </p>
      <button @click="router.push('/swipe')" class="btn-primary">
        <span>Start Swiping</span>
      </button>
    </div>
  </div>
</template>
