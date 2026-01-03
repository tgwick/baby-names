<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useSessionStore } from '@/stores/session'

const router = useRouter()
const sessionStore = useSessionStore()

const joinCode = ref('')
const loading = ref(false)
const error = ref('')

const codeChars = computed(() => {
  const chars = joinCode.value.split('')
  return [...chars, ...Array(6 - chars.length).fill('')]
})

async function handleJoin() {
  if (joinCode.value.length !== 6) {
    error.value = 'Please enter the complete 6-character code'
    return
  }

  error.value = ''
  loading.value = true

  try {
    await sessionStore.joinByCode(joinCode.value)
    router.push('/session')
  } catch (e: unknown) {
    const err = e as { response?: { data?: { errors?: string[] } } }
    error.value = err.response?.data?.errors?.[0] || 'Invalid code. Please check and try again.'
  } finally {
    loading.value = false
  }
}

function handleInput(event: Event) {
  const input = event.target as HTMLInputElement
  joinCode.value = input.value.toUpperCase().replace(/[^A-Z0-9]/g, '').slice(0, 6)
}

function handleKeydown(event: KeyboardEvent) {
  if (event.key === 'Enter' && joinCode.value.length === 6) {
    handleJoin()
  }
}
</script>

<template>
  <div class="max-w-lg mx-auto">
    <div class="card-elevated p-8 md:p-10 animate-slide-up">
      <!-- Header -->
      <div class="text-center mb-8">
        <div class="inline-flex items-center justify-center w-16 h-16 rounded-full bg-[var(--color-blush)] mb-4 animate-bounce-in">
          <span class="text-3xl">🔗</span>
        </div>
        <h1 class="font-display text-3xl font-semibold text-[var(--color-warm-gray)] mb-2">
          Join Your Partner
        </h1>
        <p class="text-[var(--color-warm-gray-light)]">
          Enter the 6-character code they shared with you
        </p>
      </div>

      <!-- Error Message -->
      <div v-if="error" class="error-message mb-6 animate-slide-up">
        {{ error }}
      </div>

      <!-- Code Input Display -->
      <div class="mb-8">
        <!-- Visual code boxes -->
        <div class="flex justify-center gap-2 mb-4">
          <div
            v-for="(char, index) in codeChars"
            :key="index"
            class="w-12 h-14 md:w-14 md:h-16 rounded-xl border-3 flex items-center justify-center font-display text-2xl font-bold transition-all duration-200"
            :class="[
              char
                ? 'border-[var(--color-coral)] bg-[var(--color-blush)] text-[var(--color-coral)]'
                : 'border-[var(--color-peach-light)] bg-[var(--color-cream)]',
              index === joinCode.length && !char ? 'animate-pulse-soft' : '',
            ]"
            :style="{ animationDelay: `${index * 0.05}s` }"
          >
            {{ char }}
          </div>
        </div>

        <!-- Hidden actual input -->
        <input
          :value="joinCode"
          @input="handleInput"
          @keydown="handleKeydown"
          type="text"
          maxlength="6"
          placeholder="Enter code..."
          class="code-input w-full opacity-0 absolute"
          style="height: 1px; overflow: hidden;"
          autofocus
        />

        <!-- Clickable overlay to focus input -->
        <div
          @click="($refs.codeInput as HTMLInputElement)?.focus()"
          class="cursor-text"
        >
          <input
            ref="codeInput"
            :value="joinCode"
            @input="handleInput"
            @keydown="handleKeydown"
            type="text"
            maxlength="6"
            class="w-full text-center text-2xl font-display font-semibold tracking-[0.5em] py-4 px-6 rounded-xl border-2 border-[var(--color-peach-light)] bg-[var(--color-cream)] text-[var(--color-coral)] focus:outline-none focus:border-[var(--color-coral)] focus:ring-4 focus:ring-[var(--color-coral)]/10 transition-all uppercase placeholder:text-[var(--color-peach-light)] placeholder:tracking-[0.3em]"
            placeholder="XXXXXX"
            autofocus
          />
        </div>
      </div>

      <!-- Join Button -->
      <button
        @click="handleJoin"
        :disabled="loading || joinCode.length !== 6"
        class="btn-primary w-full text-center"
      >
        <span>{{ loading ? 'Connecting...' : 'Join Session' }}</span>
      </button>

      <!-- Helper text -->
      <p class="text-center text-sm text-[var(--color-warm-gray-light)] mt-4">
        Ask your partner for their session code or link
      </p>

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
