<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useSessionStore } from '@/stores/session'
import { VoteType } from '@/types/vote'
import NameCard from '@/components/NameCard.vue'
import MatchCelebration from '@/components/MatchCelebration.vue'

const router = useRouter()
const sessionStore = useSessionStore()

const cardAnimating = ref<'like' | 'dislike' | null>(null)

const matchCount = computed(() => sessionStore.stats?.matchCount ?? sessionStore.matches.length)

onMounted(async () => {
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
})

async function vote(voteType: VoteType) {
  if (!sessionStore.currentName || sessionStore.voteLoading || cardAnimating.value) return

  // Start animation
  cardAnimating.value = voteType === VoteType.Like ? 'like' : 'dislike'

  // Wait for animation
  await new Promise(resolve => setTimeout(resolve, 300))

  try {
    await sessionStore.submitVote(sessionStore.currentName.id, voteType)

    // Reset animation and fetch next
    cardAnimating.value = null
    await sessionStore.fetchNextName()
  } catch {
    cardAnimating.value = null
  }
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
    <div class="flex items-center justify-between mb-6 animate-slide-up">
      <button
        @click="router.push('/session')"
        class="flex items-center gap-2 text-[var(--color-warm-gray-light)] hover:text-[var(--color-coral)] transition-colors"
      >
        <span class="text-xl">←</span>
        <span class="font-medium">Back</span>
      </button>

      <button
        @click="goToMatches"
        class="flex items-center gap-2 px-4 py-2 bg-[var(--color-blush)] rounded-full hover:bg-[var(--color-peach-light)] transition-colors"
      >
        <span class="text-xl">💕</span>
        <span class="font-semibold text-[var(--color-coral)]">{{ matchCount }}</span>
        <span class="text-[var(--color-warm-gray-light)] text-sm">matches</span>
      </button>
    </div>

    <!-- Stats bar -->
    <div
      v-if="sessionStore.stats"
      class="card p-4 mb-6 animate-slide-up stagger-1"
      style="animation-fill-mode: forwards; opacity: 0;"
    >
      <div class="grid grid-cols-3 gap-4 text-center">
        <div>
          <p class="text-2xl font-display font-bold text-[#4CAF50]">{{ sessionStore.stats.likeCount }}</p>
          <p class="text-xs text-[var(--color-warm-gray-light)]">Liked</p>
        </div>
        <div>
          <p class="text-2xl font-display font-bold text-[var(--color-coral)]">{{ sessionStore.stats.dislikeCount }}</p>
          <p class="text-xs text-[var(--color-warm-gray-light)]">Passed</p>
        </div>
        <div>
          <p class="text-2xl font-display font-bold text-[var(--color-warm-gray)]">{{ sessionStore.stats.namesRemaining }}</p>
          <p class="text-xs text-[var(--color-warm-gray-light)]">Remaining</p>
        </div>
      </div>
    </div>

    <!-- Main content -->
    <div class="relative mb-8">
      <!-- Loading state -->
      <div v-if="sessionStore.nameLoading && !sessionStore.currentName" class="text-center py-20">
        <div class="inline-flex items-center justify-center w-20 h-20 rounded-full bg-[var(--color-blush)] mb-6 animate-pulse-soft">
          <span class="text-4xl">💫</span>
        </div>
        <p class="text-[var(--color-warm-gray-light)] font-medium">Finding names...</p>
      </div>

      <!-- No more names -->
      <div v-else-if="sessionStore.noMoreNames" class="card-elevated p-10 text-center animate-bounce-in">
        <div class="w-24 h-24 mx-auto rounded-full bg-[var(--color-mint)] flex items-center justify-center mb-6">
          <span class="text-5xl">🎉</span>
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
      class="flex justify-center gap-8 animate-slide-up stagger-3"
      style="animation-fill-mode: forwards; opacity: 0;"
    >
      <!-- Dislike button -->
      <button
        @click="vote(VoteType.Dislike)"
        :disabled="sessionStore.voteLoading || cardAnimating !== null"
        class="vote-btn vote-btn-dislike"
        aria-label="Dislike"
      >
        <span class="vote-btn-icon">👎</span>
        <span class="vote-btn-label">Pass</span>
      </button>

      <!-- Like button -->
      <button
        @click="vote(VoteType.Like)"
        :disabled="sessionStore.voteLoading || cardAnimating !== null"
        class="vote-btn vote-btn-like"
        aria-label="Like"
      >
        <span class="vote-btn-icon">💗</span>
        <span class="vote-btn-label">Love it!</span>
      </button>
    </div>

    <!-- Keyboard hint -->
    <p
      v-if="sessionStore.currentName && !sessionStore.noMoreNames"
      class="text-center text-sm text-[var(--color-warm-gray-light)] mt-6 animate-fade-in"
      style="animation-delay: 0.5s;"
    >
      Tip: Use <kbd class="kbd">←</kbd> to pass and <kbd class="kbd">→</kbd> to love
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
  gap: 8px;
  padding: 20px 32px;
  border-radius: 24px;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  border: 3px solid transparent;
}

.vote-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
  transform: none !important;
}

.vote-btn-icon {
  font-size: 3rem;
  transition: transform 0.2s ease;
}

.vote-btn:hover:not(:disabled) .vote-btn-icon {
  transform: scale(1.2);
}

.vote-btn-label {
  font-weight: 600;
  font-size: 0.875rem;
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
