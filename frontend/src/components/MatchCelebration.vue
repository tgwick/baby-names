<script setup lang="ts">
import type { Match } from '@/types/vote'

const props = defineProps<{
  match: Match
}>()

const emit = defineEmits<{
  close: []
}>()
</script>

<template>
  <Teleport to="body">
    <div class="match-overlay" @click.self="emit('close')">
      <div class="match-modal animate-bounce-in">
        <!-- Confetti background -->
        <div class="confetti-container">
          <span v-for="i in 20" :key="i" class="confetti" :style="{ '--delay': `${i * 0.1}s`, '--x': `${Math.random() * 100}%` }">
            {{ ['🎉', '🐣', '✨', '🥚', '💕'][i % 5] }}
          </span>
        </div>

        <!-- Content -->
        <div class="relative z-10 text-center px-8 py-10">
          <!-- Hatch animation -->
          <div class="match-hearts mb-6">
            <span class="heart heart-left animate-float">🥚</span>
            <span class="heart heart-center text-6xl">🐣</span>
            <span class="heart heart-right animate-float" style="animation-delay: 0.5s;">🥚</span>
          </div>

          <h2 class="font-display text-4xl font-bold text-[var(--color-coral)] mb-2">
            It Hatched!
          </h2>
          <p class="text-[var(--color-warm-gray-light)] mb-6">
            You both love this name!
          </p>

          <!-- The matched name -->
          <div class="bg-[var(--color-cream)] rounded-2xl p-6 mb-8">
            <p class="font-display text-5xl font-bold text-[var(--color-warm-gray)]">
              {{ match.nameText }}
            </p>
            <p v-if="match.origin" class="text-[var(--color-warm-gray-light)] mt-2">
              Origin: {{ match.origin }}
            </p>
          </div>

          <button
            @click="emit('close')"
            class="btn-primary w-full"
          >
            <span>Keep Swiping!</span>
          </button>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<style scoped>
.match-overlay {
  position: fixed;
  inset: 0;
  background: rgba(107, 94, 87, 0.6);
  backdrop-filter: blur(8px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 100;
  padding: 20px;
}

.match-modal {
  background: linear-gradient(180deg, #FFFFFF 0%, #FFF5F3 100%);
  border-radius: 32px;
  max-width: 400px;
  width: 100%;
  position: relative;
  overflow: hidden;
  box-shadow:
    0 24px 48px -12px rgba(255, 123, 107, 0.3),
    0 48px 96px -24px rgba(107, 94, 87, 0.2);
}

.confetti-container {
  position: absolute;
  inset: 0;
  overflow: hidden;
  pointer-events: none;
}

.confetti {
  position: absolute;
  top: -20px;
  left: var(--x);
  font-size: 1.5rem;
  animation: confetti-fall 3s linear var(--delay) infinite;
}

@keyframes confetti-fall {
  0% {
    transform: translateY(-20px) rotate(0deg);
    opacity: 1;
  }
  100% {
    transform: translateY(400px) rotate(720deg);
    opacity: 0;
  }
}

.match-hearts {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
}

.heart {
  font-size: 2.5rem;
}

.heart-left {
  transform: rotate(-15deg);
}

.heart-right {
  transform: rotate(15deg);
}

.heart-center {
  animation: pulse-soft 1s ease-in-out infinite;
}
</style>
