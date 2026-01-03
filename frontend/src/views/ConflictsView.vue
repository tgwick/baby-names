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

// Split conflicts into two groups
const myLikesTheirDislikes = computed(() =>
  sessionStore.conflicts.filter((c) => c.iLikedIt)
)

const theirLikesMyDislikes = computed(() =>
  sessionStore.conflicts.filter((c) => !c.iLikedIt)
)

const partnerName = computed(() => {
  if (!sessionStore.session) return 'Your partner'
  return sessionStore.session.isInitiator
    ? sessionStore.session.partnerDisplayName
    : sessionStore.session.initiatorDisplayName
})

onMounted(async () => {
  await sessionStore.fetchCurrentSession()

  if (!sessionStore.hasSession || !sessionStore.isActive) {
    router.push('/dashboard')
    return
  }

  await sessionStore.fetchConflicts()
})

async function handleClearDislike(nameId: number) {
  await sessionStore.clearDislike(nameId)
}

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
      <div class="inline-flex items-center justify-center w-16 h-16 sm:w-20 sm:h-20 rounded-full bg-[var(--color-lavender)] mb-3 sm:mb-4">
        <span class="text-3xl sm:text-4xl">💔</span>
      </div>
      <h1 class="font-display text-3xl sm:text-4xl font-bold text-[var(--color-warm-gray)]">
        Conflicts
      </h1>
      <p class="text-sm sm:text-base text-[var(--color-warm-gray-light)] mt-2">
        {{ sessionStore.conflicts.length }} {{ sessionStore.conflicts.length === 1 ? 'name' : 'names' }} where you disagree
      </p>
    </div>

    <!-- Loading state -->
    <div v-if="sessionStore.conflictLoading && sessionStore.conflicts.length === 0" class="text-center py-16">
      <div class="inline-flex items-center justify-center w-16 h-16 rounded-full bg-[var(--color-lavender)] mb-4 animate-pulse-soft">
        <span class="text-3xl">🔄</span>
      </div>
      <p class="text-[var(--color-warm-gray-light)]">Loading conflicts...</p>
    </div>

    <!-- Conflicts list -->
    <div v-else-if="sessionStore.conflicts.length > 0" class="space-y-6 sm:space-y-8">
      <!-- My Likes, Their Dislikes Section -->
      <div v-if="myLikesTheirDislikes.length > 0" class="animate-slide-up stagger-2" style="animation-fill-mode: forwards; opacity: 0;">
        <div class="flex items-center gap-2.5 sm:gap-3 mb-3 sm:mb-4">
          <div class="w-9 h-9 sm:w-10 sm:h-10 rounded-lg sm:rounded-xl bg-[var(--color-mint)] flex items-center justify-center flex-shrink-0">
            <span class="text-lg sm:text-xl">💚</span>
          </div>
          <div>
            <h2 class="font-display text-lg sm:text-xl font-semibold text-[var(--color-warm-gray)]">Names You Love</h2>
            <p class="text-xs sm:text-sm text-[var(--color-warm-gray-light)]">{{ partnerName }} passed on these</p>
          </div>
        </div>

        <div class="space-y-2.5 sm:space-y-3">
          <div
            v-for="(conflict, index) in myLikesTheirDislikes"
            :key="conflict.nameId"
            class="card p-3.5 sm:p-5 border-l-4 border-[var(--color-mint)]"
            :style="`animation-delay: ${0.1 + index * 0.05}s;`"
          >
            <div class="flex items-center gap-3 sm:gap-4">
              <!-- Icon -->
              <div class="w-11 h-11 sm:w-14 sm:h-14 rounded-xl sm:rounded-2xl bg-gradient-to-br from-[var(--color-mint)] to-[#98D9C2] flex items-center justify-center flex-shrink-0">
                <span class="text-xl sm:text-2xl">{{ genderIcon(conflict.gender) }}</span>
              </div>

              <!-- Name info -->
              <div class="flex-1 min-w-0">
                <h3 class="font-display text-lg sm:text-xl font-semibold text-[var(--color-warm-gray)] truncate">
                  {{ conflict.nameText }}
                </h3>
                <div class="flex flex-wrap items-center gap-1.5 sm:gap-2 mt-0.5 sm:mt-1">
                  <span v-if="conflict.origin" class="text-xs sm:text-sm text-[var(--color-warm-gray-light)]">
                    {{ conflict.origin }}
                  </span>
                  <span class="text-xs text-[var(--color-warm-gray-light)]">
                    • {{ formatDate(conflict.conflictedAt) }}
                  </span>
                </div>
              </div>

              <!-- Status -->
              <div class="flex flex-col items-center gap-0.5 sm:gap-1 flex-shrink-0">
                <span class="text-lg sm:text-xl">💚</span>
                <span class="text-xs text-[var(--color-warm-gray-light)] hidden sm:block">You liked</span>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Their Likes, My Dislikes Section -->
      <div v-if="theirLikesMyDislikes.length > 0" class="animate-slide-up stagger-3" style="animation-fill-mode: forwards; opacity: 0;">
        <div class="flex items-center gap-2.5 sm:gap-3 mb-3 sm:mb-4">
          <div class="w-9 h-9 sm:w-10 sm:h-10 rounded-lg sm:rounded-xl bg-[var(--color-blush)] flex items-center justify-center flex-shrink-0">
            <span class="text-lg sm:text-xl">🔄</span>
          </div>
          <div>
            <h2 class="font-display text-lg sm:text-xl font-semibold text-[var(--color-warm-gray)]">Reconsider These?</h2>
            <p class="text-xs sm:text-sm text-[var(--color-warm-gray-light)]">{{ partnerName }} loves these names</p>
          </div>
        </div>

        <div class="space-y-2.5 sm:space-y-3">
          <div
            v-for="(conflict, index) in theirLikesMyDislikes"
            :key="conflict.nameId"
            class="card p-3.5 sm:p-5 border-l-4 border-[var(--color-peach)]"
            :style="`animation-delay: ${0.15 + index * 0.05}s;`"
          >
            <div class="flex items-center gap-3 sm:gap-4">
              <!-- Icon -->
              <div class="w-11 h-11 sm:w-14 sm:h-14 rounded-xl sm:rounded-2xl bg-gradient-to-br from-[var(--color-peach-light)] to-[var(--color-peach)] flex items-center justify-center flex-shrink-0">
                <span class="text-xl sm:text-2xl">{{ genderIcon(conflict.gender) }}</span>
              </div>

              <!-- Name info -->
              <div class="flex-1 min-w-0">
                <h3 class="font-display text-lg sm:text-xl font-semibold text-[var(--color-warm-gray)] truncate">
                  {{ conflict.nameText }}
                </h3>
                <div class="flex flex-wrap items-center gap-1.5 sm:gap-2 mt-0.5 sm:mt-1">
                  <span v-if="conflict.origin" class="text-xs sm:text-sm text-[var(--color-warm-gray-light)]">
                    {{ conflict.origin }}
                  </span>
                  <span class="text-xs text-[var(--color-warm-gray-light)]">
                    • {{ formatDate(conflict.conflictedAt) }}
                  </span>
                </div>
              </div>

              <!-- Action -->
              <button
                @click="handleClearDislike(conflict.nameId)"
                :disabled="sessionStore.conflictLoading"
                class="flex flex-col items-center gap-0.5 sm:gap-1 px-2.5 sm:px-4 py-2 rounded-lg sm:rounded-xl bg-[var(--color-cream)] hover:bg-[var(--color-mint)] transition-colors group min-h-[44px] min-w-[60px] sm:min-w-[80px] flex-shrink-0"
              >
                <span class="text-lg sm:text-xl group-hover:scale-110 transition-transform">💚</span>
                <span class="text-xs font-medium text-[var(--color-warm-gray)] group-hover:text-emerald-700 whitespace-nowrap">Try it</span>
              </button>
            </div>

            <!-- Partner badge -->
            <div class="mt-2.5 sm:mt-3 pt-2.5 sm:pt-3 border-t border-[var(--color-cream-dark)]">
              <div class="flex items-center gap-2 text-xs sm:text-sm text-[var(--color-warm-gray-light)]">
                <span>💗</span>
                <span>{{ partnerName }} loved this name</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Empty state -->
    <div v-else class="card-elevated p-6 sm:p-10 text-center animate-slide-up stagger-2" style="animation-fill-mode: forwards; opacity: 0;">
      <div class="w-20 h-20 sm:w-24 sm:h-24 mx-auto rounded-full bg-[var(--color-mint)] flex items-center justify-center mb-4 sm:mb-6">
        <span class="text-4xl sm:text-5xl">🎉</span>
      </div>
      <h2 class="font-display text-xl sm:text-2xl font-semibold text-[var(--color-warm-gray)] mb-2 sm:mb-3">
        No Conflicts!
      </h2>
      <p class="text-sm sm:text-base text-[var(--color-warm-gray-light)] mb-4 sm:mb-6 max-w-sm mx-auto">
        You and {{ partnerName }} are in perfect harmony. Keep swiping to find more matches!
      </p>
      <button @click="router.push('/swipe')" class="btn-primary">
        <span>Continue Swiping</span>
      </button>
    </div>

    <!-- Info card -->
    <div class="card p-4 sm:p-6 mt-6 sm:mt-8 animate-slide-up stagger-4" style="animation-fill-mode: forwards; opacity: 0;">
      <div class="flex items-start gap-3 sm:gap-4">
        <div class="w-9 h-9 sm:w-10 sm:h-10 rounded-lg sm:rounded-xl bg-[var(--color-sky)] flex items-center justify-center flex-shrink-0">
          <span class="text-lg sm:text-xl">💡</span>
        </div>
        <div>
          <h3 class="font-display font-semibold text-[var(--color-warm-gray)] mb-1 text-sm sm:text-base">How Conflicts Work</h3>
          <p class="text-xs sm:text-sm text-[var(--color-warm-gray-light)] leading-relaxed">
            When you and {{ partnerName }} disagree on a name, it shows up here. If you passed on a name they loved,
            you can give it another chance by clicking "Try it" — the name will return to your swiping queue.
          </p>
        </div>
      </div>
    </div>
  </div>
</template>
