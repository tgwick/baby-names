<script setup lang="ts">
import { computed } from 'vue'
import { usePreferencesStore } from '@/stores/preferences'

const emit = defineEmits<{
  complete: []
}>()

const store = usePreferencesStore()

const selectedOptions = computed(() => {
  if (!store.currentQuestion) return []
  return store.getAnswer(store.currentQuestion.questionId)
})

function isSelected(optionId: string): boolean {
  return selectedOptions.value.includes(optionId)
}

function toggleOption(optionId: string) {
  if (!store.currentQuestion) return

  const current = [...selectedOptions.value]
  const index = current.indexOf(optionId)

  if (store.currentQuestion.allowMultiple) {
    // Multi-select: toggle the option
    if (index > -1) {
      current.splice(index, 1)
    } else {
      // If selecting "no preference", clear others
      if (optionId === 'no_pref') {
        store.setAnswer(store.currentQuestion.questionId, [optionId])
        return
      }
      // If selecting something else, remove "no preference"
      const noPrefIndex = current.indexOf('no_pref')
      if (noPrefIndex > -1) {
        current.splice(noPrefIndex, 1)
      }
      current.push(optionId)
    }
  } else {
    // Single-select: replace
    store.setAnswer(store.currentQuestion.questionId, [optionId])
    return
  }

  store.setAnswer(store.currentQuestion.questionId, current)
}

function handleNext() {
  if (store.isLastQuestion) {
    emit('complete')
  } else {
    store.nextQuestion()
  }
}

function handleSkip() {
  // Set "no preference" or empty and move on
  if (store.currentQuestion) {
    const noPref = store.currentQuestion.options.find(o => o.optionId === 'no_pref')
    if (noPref) {
      store.setAnswer(store.currentQuestion.questionId, ['no_pref'])
    } else {
      store.setAnswer(store.currentQuestion.questionId, [])
    }
  }
  handleNext()
}
</script>

<template>
  <div class="questionnaire">
    <!-- Progress bar -->
    <div class="mb-6">
      <div class="flex justify-between text-sm text-[var(--color-warm-gray-light)] mb-2">
        <span>Question {{ store.currentQuestionIndex + 1 }} of {{ store.totalQuestions }}</span>
        <span>{{ Math.round(store.progress) }}% complete</span>
      </div>
      <div class="h-2 bg-[var(--color-cream)] rounded-full overflow-hidden">
        <div
          class="h-full bg-gradient-to-r from-[var(--color-peach)] to-[var(--color-coral)] transition-all duration-300"
          :style="{ width: `${store.progress}%` }"
        />
      </div>
    </div>

    <!-- Question -->
    <div v-if="store.currentQuestion" class="space-y-6">
      <h2 class="text-xl sm:text-2xl font-semibold text-[var(--color-warm-gray)] text-center">
        {{ store.currentQuestion.questionText }}
      </h2>

      <p v-if="store.currentQuestion.allowMultiple" class="text-center text-sm text-[var(--color-warm-gray-light)]">
        Select all that apply
      </p>

      <!-- Options -->
      <div class="space-y-3">
        <button
          v-for="option in store.currentQuestion.options"
          :key="option.optionId"
          @click="toggleOption(option.optionId)"
          class="option-button"
          :class="{ selected: isSelected(option.optionId) }"
        >
          <div class="flex items-center gap-3">
            <div class="checkbox">
              <svg
                v-if="isSelected(option.optionId)"
                class="w-4 h-4 text-white"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
              >
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="3" d="M5 13l4 4L19 7" />
              </svg>
            </div>
            <div class="text-left">
              <div class="font-medium">{{ option.label }}</div>
              <div v-if="option.description" class="text-sm opacity-75">
                {{ option.description }}
              </div>
            </div>
          </div>
        </button>
      </div>

      <!-- Navigation -->
      <div class="flex gap-3 pt-4">
        <button
          v-if="!store.isFirstQuestion"
          @click="store.previousQuestion()"
          class="flex-1 py-3 px-4 rounded-xl font-semibold border-2 border-[var(--color-warm-gray-light)] text-[var(--color-warm-gray-light)] hover:bg-[var(--color-cream)] transition-colors"
        >
          Back
        </button>
        <button
          @click="handleSkip"
          class="flex-1 py-3 px-4 rounded-xl font-semibold text-[var(--color-warm-gray-light)] hover:bg-[var(--color-cream)] transition-colors"
        >
          Skip
        </button>
        <button
          @click="handleNext"
          :disabled="selectedOptions.length === 0"
          class="flex-1 py-3 px-4 rounded-xl font-semibold bg-gradient-to-r from-[var(--color-peach)] to-[var(--color-coral)] text-white shadow-md hover:shadow-lg transition-all disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {{ store.isLastQuestion ? 'Finish' : 'Next' }}
        </button>
      </div>
    </div>

    <!-- Loading state -->
    <div v-else-if="store.loading" class="text-center py-12">
      <div class="animate-spin w-8 h-8 border-4 border-[var(--color-peach)] border-t-transparent rounded-full mx-auto mb-4" />
      <p class="text-[var(--color-warm-gray-light)]">Loading questions...</p>
    </div>
  </div>
</template>

<style scoped>
.questionnaire {
  max-width: 500px;
  margin: 0 auto;
}

.option-button {
  width: 100%;
  padding: 1rem 1.25rem;
  background: white;
  border: 2px solid var(--color-cream);
  border-radius: 1rem;
  transition: all 0.2s;
  text-align: left;
  color: var(--color-warm-gray);
}

.option-button:hover {
  border-color: var(--color-peach-light);
  background: var(--color-cream);
}

.option-button.selected {
  border-color: var(--color-coral);
  background: linear-gradient(135deg, rgba(255, 123, 107, 0.1) 0%, rgba(255, 173, 159, 0.1) 100%);
}

.checkbox {
  width: 1.5rem;
  height: 1.5rem;
  border-radius: 0.5rem;
  border: 2px solid var(--color-warm-gray-light);
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  transition: all 0.2s;
}

.option-button.selected .checkbox {
  background: linear-gradient(135deg, var(--color-coral) 0%, var(--color-peach) 100%);
  border-color: var(--color-coral);
}
</style>
