<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useSessionStore } from '@/stores/session'
import { VoteType } from '@/types/vote'
import { extractApiError } from '@/services/api'
import NameCard from '@/components/NameCard.vue'
import MatchCelebration from '@/components/MatchCelebration.vue'

const router = useRouter()
const sessionStore = useSessionStore()

const cardAnimating = ref<'like' | 'dislike' | null>(null)
const error = ref<string | null>(null)
const initialLoading = ref(true)

const matchCount = computed(() => sessionStore.stats?.matchCount ?? sessionStore.matches.length)

async function loadInitialData() {
  error.value = null
  initialLoading.value = true

  try {
    await sessionStore.fetchCurrentSession()

    if (!sessionStore.hasSession) {
      router.push('/dashboard')
      return
    }

    if (!sessionStore.isActive) {
      router.push('/session')
      return
    }

    // Fetch initial data
    await Promise.all([
      sessionStore.fetchNextName(),
      sessionStore.fetchStats(),
      sessionStore.fetchMatches(),
    ])
  } catch (e) {
    error.value = extractApiError(e).message
  } finally {
    initialLoading.value = false
  }
}

// Keyboard navigation for voting
function handleKeydown(event: KeyboardEvent) {
  // Don't trigger if user is typing in an input
  if (event.target instanceof HTMLInputElement || event.target instanceof HTMLTextAreaElement) {
    return
  }

  if (!sessionStore.currentName || sessionStore.noMoreNames) return

  if (event.key === 'ArrowLeft') {
    event.preventDefault()
    vote(VoteType.Dislike)
  } else if (event.key === 'ArrowRight') {
    event.preventDefault()
    vote(VoteType.Like)
  }
}

onMounted(() => {
  loadInitialData()
  window.addEventListener('keydown', handleKeydown)
})

onUnmounted(() => {
  window.removeEventListener('keydown', handleKeydown)
})

async function vote(voteType: VoteType) {
  if (!sessionStore.currentName || sessionStore.voteLoading || cardAnimating.value) return

  error.value = null

  // Start animation
  cardAnimating.value = voteType === VoteType.Like ? 'like' : 'dislike'

  // Wait for animation
  await new Promise(resolve => setTimeout(resolve, 300))

  try {
    await sessionStore.submitVote(sessionStore.currentName.id, voteType)

    // Reset animation and fetch next
    cardAnimating.value = null
    await sessionStore.fetchNextName()
  } catch (e) {
    cardAnimating.value = null
    error.value = extractApiError(e).message
  }
}

function dismissError() {
  error.value = null
}

function handleMatchClose() {
  sessionStore.clearNewMatch()
}

function goToMatches() {
  router.push('/matches')
}
</script>

<template>
  <div class="max-w-lg mx-auto">
    <!-- Header with stats -->
    <div class="flex items-center justify-between mb-4 sm:mb-6 animate-slide-up">
      <button
        @click="router.push('/session')"
        class="flex items-center gap-2 text-[var(--color-warm-gray-light)] hover:text-[var(--color-coral)] transition-colors min-h-[44px] min-w-[44px] px-2"
      >
        <span class="text-xl">←</span>
        <span class="font-medium">Back</span>
      </button>

      <button
        @click="goToMatches"
        class="flex items-center gap-1.5 sm:gap-2 px-3 sm:px-4 py-2 bg-[var(--color-blush)] rounded-full hover:bg-[var(--color-peach-light)] transition-colors min-h-[44px]"
      >
        <span class="text-lg sm:text-xl">💕</span>
        <span class="font-semibold text-[var(--color-coral)]">{{ matchCount }}</span>
        <span class="text-[var(--color-warm-gray-light)] text-xs sm:text-sm">matches</span>
      </button>
    </div>

    <!-- Stats bar -->
    <div
      v-if="sessionStore.stats"
      class="card p-3 sm:p-4 mb-4 sm:mb-6 animate-slide-up stagger-1"
      style="animation-fill-mode: forwards; opacity: 0;"
    >
      <div class="grid grid-cols-3 gap-2 sm:gap-4 text-center">
        <div>
          <p class="text-xl sm:text-2xl font-display font-bold text-[#4CAF50]">{{ sessionStore.stats.likeCount }}</p>
          <p class="text-xs text-[var(--color-warm-gray-light)]">Liked</p>
        </div>
        <div>
          <p class="text-xl sm:text-2xl font-display font-bold text-[var(--color-coral)]">{{ sessionStore.stats.dislikeCount }}</p>
          <p class="text-xs text-[var(--color-warm-gray-light)]">Passed</p>
        </div>
        <div>
          <p class="text-xl sm:text-2xl font-display font-bold text-[var(--color-warm-gray)]">{{ sessionStore.stats.namesRemaining }}</p>
          <p class="text-xs text-[var(--color-warm-gray-light)]">Remaining</p>
        </div>
      </div>
    </div>

    <!-- Error toast -->
    <div
      v-if="error"
      role="alert"
      aria-live="polite"
      class="card border-l-4 border-[var(--color-coral)] p-3 sm:p-4 mb-4 sm:mb-6 bg-red-50 animate-slide-up"
    >
      <div class="flex items-start gap-3">
        <span class="text-lg sm:text-xl flex-shrink-0">⚠️</span>
        <div class="flex-1 min-w-0">
          <p class="text-sm sm:text-base text-[var(--color-coral)] font-medium">{{ error }}</p>
        </div>
        <button
          @click="dismissError"
          class="text-[var(--color-warm-gray-light)] hover:text-[var(--color-coral)] transition-colors p-1 min-h-[44px] min-w-[44px] flex items-center justify-center"
          aria-label="Dismiss error"
        >
          ✕
        </button>
      </div>
    </div>

    <!-- Main content -->
    <div class="relative mb-6 sm:mb-8">
      <!-- Initial loading state -->
      <div v-if="initialLoading" class="text-center py-20">
        <div class="inline-flex items-center justify-center w-20 h-20 rounded-full bg-[var(--color-blush)] mb-6 animate-pulse-soft">
          <span class="text-4xl">💫</span>
        </div>
        <p class="text-[var(--color-warm-gray-light)] font-medium">Loading...</p>
      </div>

      <!-- Error state with retry -->
      <div v-else-if="error && !sessionStore.currentName" class="card-elevated p-6 sm:p-10 text-center animate-bounce-in">
        <div class="w-20 h-20 sm:w-24 sm:h-24 mx-auto rounded-full bg-red-100 flex items-center justify-center mb-4 sm:mb-6">
          <span class="text-4xl sm:text-5xl">😢</span>
        </div>
        <h2 class="font-display text-xl sm:text-2xl font-semibold text-[var(--color-warm-gray)] mb-2 sm:mb-3">
          Something went wrong
        </h2>
        <p class="text-sm sm:text-base text-[var(--color-warm-gray-light)] mb-4 sm:mb-6">
          {{ error }}
        </p>
        <button @click="loadInitialData" class="btn-primary">
          <span>Try Again</span>
        </button>
      </div>

      <!-- Loading state while fetching next name -->
      <div v-else-if="sessionStore.nameLoading && !sessionStore.currentName" role="status" aria-live="polite" class="text-center py-20">
        <div class="inline-flex items-center justify-center w-20 h-20 rounded-full bg-[var(--color-blush)] mb-6 animate-pulse-soft">
          <span class="text-4xl" aria-hidden="true">💫</span>
        </div>
        <p class="text-[var(--color-warm-gray-light)] font-medium">Finding names...</p>
      </div>

      <!-- No more names -->
      <div v-else-if="sessionStore.noMoreNames" class="card-elevated p-10 text-center animate-bounce-in">
        <div class="w-24 h-24 mx-auto rounded-full bg-[var(--color-mint)] flex items-center justify-center mb-6">
          <span class="text-5xl" aria-hidden="true">🎉</span>
        </div>
        <h2 class="font-display text-3xl font-semibold text-[var(--color-warm-gray)] mb-3">
          All Done!
        </h2>
        <p class="text-[var(--color-warm-gray-light)] mb-6">
          You've voted on all available names. Check out your matches!
        </p>
        <button @click="goToMatches" class="btn-primary">
          <span>View {{ matchCount }} Matches</span>
        </button>
      </div>

      <!-- Name card -->
      <div
        v-else-if="sessionStore.currentName"
        class="swipe-card-container animate-slide-up stagger-2"
        style="animation-fill-mode: forwards; opacity: 0;"
      >
        <div
          class="swipe-card"
          :class="{
            'swipe-left': cardAnimating === 'dislike',
            'swipe-right': cardAnimating === 'like'
          }"
        >
          <NameCard
            :name="sessionStore.currentName"
            :loading="sessionStore.voteLoading"
          />
        </div>
      </div>
    </div>

    <!-- Vote buttons -->
    <div
      v-if="sessionStore.currentName && !sessionStore.noMoreNames"
      class="flex justify-center gap-4 sm:gap-6 md:gap-8 animate-slide-up stagger-3"
      style="animation-fill-mode: forwards; opacity: 0;"
    >
      <!-- Dislike button -->
      <button
        @click="vote(VoteType.Dislike)"
        :disabled="sessionStore.voteLoading || cardAnimating !== null"
        class="vote-btn vote-btn-dislike"
        :aria-label="`Pass on ${sessionStore.currentName?.nameText ?? 'this name'} (keyboard: left arrow)`"
      >
        <span class="vote-btn-icon" aria-hidden="true">👎</span>
        <span class="vote-btn-label">Pass</span>
      </button>

      <!-- Like button -->
      <button
        @click="vote(VoteType.Like)"
        :disabled="sessionStore.voteLoading || cardAnimating !== null"
        class="vote-btn vote-btn-like"
        :aria-label="`Love ${sessionStore.currentName?.nameText ?? 'this name'} (keyboard: right arrow)`"
      >
        <span class="vote-btn-icon" aria-hidden="true">💗</span>
        <span class="vote-btn-label">Love it!</span>
      </button>
    </div>

    <!-- Keyboard hint -->
    <p
      v-if="sessionStore.currentName && !sessionStore.noMoreNames"
      class="text-center text-sm text-[var(--color-warm-gray-light)] mt-6 animate-fade-in"
      style="animation-delay: 0.5s;"
    >
      Tip: Use <kbd class="kbd" aria-label="left arrow key">←</kbd> to pass and <kbd class="kbd" aria-label="right arrow key">→</kbd> to love
    </p>

    <!-- Match celebration -->
    <MatchCelebration
      v-if="sessionStore.newMatch"
      :match="sessionStore.newMatch"
      @close="handleMatchClose"
    />
  </div>
</template>

<style scoped>
.swipe-card-container {
  perspective: 1000px;
}

.swipe-card {
  transition: transform 0.3s ease-out, opacity 0.3s ease-out;
  transform-style: preserve-3d;
}

.swipe-card.swipe-left {
  transform: translateX(-150%) rotate(-20deg);
  opacity: 0;
}

.swipe-card.swipe-right {
  transform: translateX(150%) rotate(20deg);
  opacity: 0;
}

.vote-btn {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 4px;
  padding: 16px 24px;
  border-radius: 20px;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  border: 3px solid transparent;
  min-height: 88px;
  min-width: 88px;
}

@media (min-width: 640px) {
  .vote-btn {
    gap: 8px;
    padding: 20px 32px;
    border-radius: 24px;
    min-height: 100px;
    min-width: 100px;
  }
}

.vote-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
  transform: none !important;
}

.vote-btn-icon {
  font-size: 2.25rem;
  transition: transform 0.2s ease;
}

@media (min-width: 640px) {
  .vote-btn-icon {
    font-size: 3rem;
  }
}

.vote-btn:hover:not(:disabled) .vote-btn-icon {
  transform: scale(1.2);
}

.vote-btn-label {
  font-weight: 600;
  font-size: 0.75rem;
}

@media (min-width: 640px) {
  .vote-btn-label {
    font-size: 0.875rem;
  }
}

.vote-btn-dislike {
  background: linear-gradient(135deg, #FFF0EE 0%, #FFEBE8 100%);
  color: #E85D4C;
}

.vote-btn-dislike:hover:not(:disabled) {
  background: linear-gradient(135deg, #FFE5E0 0%, #FFDDD8 100%);
  border-color: #FFAB91;
  transform: translateY(-4px) scale(1.02);
  box-shadow: 0 12px 24px -8px rgba(232, 93, 76, 0.25);
}

.vote-btn-like {
  background: linear-gradient(135deg, #E8F5E9 0%, #C8E6C9 100%);
  color: #2E7D32;
}

.vote-btn-like:hover:not(:disabled) {
  background: linear-gradient(135deg, #C8E6C9 0%, #A5D6A7 100%);
  border-color: #81C784;
  transform: translateY(-4px) scale(1.02);
  box-shadow: 0 12px 24px -8px rgba(76, 175, 80, 0.25);
}

.kbd {
  display: inline-block;
  padding: 2px 8px;
  background: var(--color-cream);
  border: 1px solid var(--color-cream-dark);
  border-radius: 6px;
  font-family: monospace;
  font-size: 0.75rem;
}
</style>
