<script setup lang="ts">
import { onMounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useSessionStore } from '@/stores/session'
import { Gender } from '@/types/session'

const router = useRouter()
const sessionStore = useSessionStore()

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

onMounted(async () => {
  await sessionStore.fetchCurrentSession()

  if (!sessionStore.hasSession || !sessionStore.isActive) {
    router.push('/dashboard')
    return
  }

  await sessionStore.fetchMatches()
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
    <div class="flex items-center justify-between mb-8 animate-slide-up">
      <button
        @click="router.push('/swipe')"
        class="flex items-center gap-2 text-[var(--color-warm-gray-light)] hover:text-[var(--color-coral)] transition-colors"
      >
        <span class="text-xl">←</span>
        <span class="font-medium">Keep Swiping</span>
      </button>
    </div>

    <!-- Title -->
    <div class="text-center mb-8 animate-slide-up stagger-1" style="animation-fill-mode: forwards; opacity: 0;">
      <div class="inline-flex items-center justify-center w-20 h-20 rounded-full bg-[var(--color-blush)] mb-4">
        <span class="text-4xl">💕</span>
      </div>
      <h1 class="font-display text-4xl font-bold text-[var(--color-warm-gray)]">
        Your Matches
      </h1>
      <p class="text-[var(--color-warm-gray-light)] mt-2">
        {{ sessionStore.matches.length }} {{ sessionStore.matches.length === 1 ? 'name' : 'names' }} you both love
      </p>
    </div>

    <!-- Matches list -->
    <div v-if="sessionStore.matches.length > 0" class="space-y-4">
      <div
        v-for="(match, index) in sortedMatches"
        :key="match.nameId"
        class="card p-6 animate-slide-up hover:shadow-lg transition-shadow"
        :style="`animation-delay: ${0.1 + index * 0.05}s; animation-fill-mode: forwards; opacity: 0;`"
      >
        <div class="flex items-center gap-4">
          <!-- Icon -->
          <div class="w-16 h-16 rounded-2xl bg-gradient-to-br from-[var(--color-peach-light)] to-[var(--color-peach)] flex items-center justify-center">
            <span class="text-3xl">{{ genderIcon(match.gender) }}</span>
          </div>

          <!-- Name info -->
          <div class="flex-1">
            <h3 class="font-display text-2xl font-semibold text-[var(--color-warm-gray)]">
              {{ match.nameText }}
            </h3>
            <div class="flex flex-wrap items-center gap-2 mt-1">
              <span v-if="match.origin" class="text-sm text-[var(--color-warm-gray-light)]">
                {{ match.origin }}
              </span>
              <span class="text-xs text-[var(--color-warm-gray-light)]">
                • Matched {{ formatDate(match.matchedAt) }}
              </span>
            </div>
          </div>

          <!-- Match indicator -->
          <div class="text-3xl animate-pulse-soft">
            💗
          </div>
        </div>

        <!-- Popularity bar -->
        <div class="mt-4 pt-4 border-t border-[var(--color-cream-dark)]">
          <div class="flex items-center justify-between text-sm mb-2">
            <span class="text-[var(--color-warm-gray-light)]">Popularity</span>
            <span class="font-semibold text-[var(--color-coral)]">{{ match.popularityScore }}%</span>
          </div>
          <div class="h-2 bg-[var(--color-cream)] rounded-full overflow-hidden">
            <div
              class="h-full bg-gradient-to-r from-[var(--color-peach)] to-[var(--color-coral)] rounded-full transition-all duration-500"
              :style="`width: ${match.popularityScore}%`"
            />
          </div>
        </div>
      </div>
    </div>

    <!-- Empty state -->
    <div v-else class="card-elevated p-10 text-center animate-slide-up stagger-2" style="animation-fill-mode: forwards; opacity: 0;">
      <div class="w-24 h-24 mx-auto rounded-full bg-[var(--color-cream)] flex items-center justify-center mb-6">
        <span class="text-5xl">💭</span>
      </div>
      <h2 class="font-display text-2xl font-semibold text-[var(--color-warm-gray)] mb-3">
        No Matches Yet
      </h2>
      <p class="text-[var(--color-warm-gray-light)] mb-6 max-w-sm mx-auto">
        Keep swiping! When you and your partner both love a name, it'll appear here.
      </p>
      <button @click="router.push('/swipe')" class="btn-primary">
        <span>Start Swiping</span>
      </button>
    </div>
  </div>
</template>
