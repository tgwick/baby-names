<script setup lang="ts">
import { computed } from 'vue'
import type { Name } from '@/types/name'
import { Gender } from '@/types/session'

const props = defineProps<{
  name: Name
  loading?: boolean
}>()

const genderIcon = computed(() => {
  switch (props.name.gender) {
    case Gender.Male:
      return '👦'
    case Gender.Female:
      return '👧'
    default:
      return '👶'
  }
})

const genderLabel = computed(() => {
  switch (props.name.gender) {
    case Gender.Male:
      return 'Boy'
    case Gender.Female:
      return 'Girl'
    default:
      return 'Neutral'
  }
})

const genderGradient = computed(() => {
  switch (props.name.gender) {
    case Gender.Male:
      return 'from-[var(--color-sky)] to-[#7BC4E0]'
    case Gender.Female:
      return 'from-[var(--color-peach-light)] to-[var(--color-peach)]'
    default:
      return 'from-[var(--color-mint)] to-[#8CD4BE]'
  }
})

const popularityStars = computed(() => {
  const score = props.name.popularityScore
  if (score >= 80) return 5
  if (score >= 60) return 4
  if (score >= 40) return 3
  if (score >= 20) return 2
  return 1
})
</script>

<template>
  <div
    class="name-card"
    :class="{ 'animate-pulse': loading }"
  >
    <!-- Gender badge -->
    <div
      class="absolute -top-4 left-1/2 -translate-x-1/2 px-6 py-2 rounded-full shadow-md"
      :class="`bg-gradient-to-r ${genderGradient}`"
    >
      <div class="flex items-center gap-2">
        <span class="text-xl">{{ genderIcon }}</span>
        <span class="font-semibold text-white text-sm">{{ genderLabel }}</span>
      </div>
    </div>

    <!-- Main content -->
    <div class="pt-8 pb-6 px-8 text-center">
      <!-- Name display -->
      <h2 class="font-display text-6xl md:text-7xl font-bold text-[var(--color-warm-gray)] mb-6 leading-tight">
        {{ name.nameText }}
      </h2>

      <!-- Details -->
      <div class="space-y-4">
        <!-- Origin -->
        <div v-if="name.origin" class="inline-flex items-center gap-2 px-4 py-2 bg-[var(--color-cream)] rounded-full">
          <span class="text-lg">🌍</span>
          <span class="text-[var(--color-warm-gray-light)] font-medium">{{ name.origin }}</span>
        </div>

        <!-- Popularity -->
        <div class="flex justify-center items-center gap-1">
          <span
            v-for="i in 5"
            :key="i"
            class="text-2xl transition-all duration-200"
            :class="i <= popularityStars ? 'grayscale-0 scale-100' : 'grayscale opacity-30 scale-90'"
          >
            {{ i <= popularityStars ? '⭐' : '☆' }}
          </span>
        </div>
        <p class="text-sm text-[var(--color-warm-gray-light)]">
          Popularity Score: {{ name.popularityScore }}%
        </p>
      </div>
    </div>

    <!-- Decorative corner elements -->
    <div class="absolute top-4 left-4 text-3xl opacity-20 rotate-[-15deg]">💫</div>
    <div class="absolute bottom-4 right-4 text-3xl opacity-20 rotate-[15deg]">✨</div>
  </div>
</template>

<style scoped>
.name-card {
  position: relative;
  background: linear-gradient(180deg, #FFFFFF 0%, #FFFAF7 100%);
  border-radius: 32px;
  box-shadow:
    0 8px 32px -8px rgba(107, 94, 87, 0.15),
    0 24px 48px -16px rgba(255, 123, 107, 0.12);
  border: 2px solid rgba(255, 173, 159, 0.2);
  overflow: visible;
  min-height: 320px;
  display: flex;
  flex-direction: column;
  justify-content: center;
}
</style>
