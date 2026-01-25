<script setup lang="ts">
import { computed } from 'vue'
import { useFiltersStore } from '@/stores/filters'

const emit = defineEmits<{
  complete: []
}>()

const store = useFiltersStore()

const selectedOption = computed(() => {
  if (!store.currentQuestion) return null
  return store.getAnswer(store.currentQuestion.questionId)
})

function isSelected(optionId: string): boolean {
  return selectedOption.value === optionId
}

function selectOption(optionId: string) {
  if (!store.currentQuestion) return
  store.setAnswer(store.currentQuestion.questionId, optionId)
}

function handleNext() {
  if (store.isLastQuestion) {
    emit('complete')
  } else {
    store.nextQuestion()
  }
}

function handleSkip() {
  // Set "no preference" and move on
  if (store.currentQuestion) {
    store.setAnswer(store.currentQuestion.questionId, 'no_pref')
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
          class="h-full bg-gradient-to-r from-[var(--color-sage)] to-[var(--color-sage-dark)] transition-all duration-300"
          :style="{ width: `${store.progress}%` }"
        />
      </div>
    </div>

    <!-- Question -->
    <div v-if="store.currentQuestion" class="space-y-6">
      <h2 class="text-xl sm:text-2xl font-semibold text-[var(--color-warm-gray)] text-center">
        {{ store.currentQuestion.questionText }}
      </h2>

      <!-- Options -->
      <div class="space-y-3">
        <button
          v-for="option in store.currentQuestion.options"
          :key="option.optionId"
          @click="selectOption(option.optionId)"
          class="filter-option-button"
          :class="{ selected: isSelected(option.optionId) }"
        >
          <div class="flex items-center gap-3">
            <div class="radio-circle">
              <div v-if="isSelected(option.optionId)" class="radio-dot" />
            </div>
            <div class="text-left flex-1">
              <div class="font-medium">{{ option.label }}</div>
              <div v-if="option.description" class="text-sm opacity-75 mt-0.5">
                {{ option.description }}
              </div>
              <div v-if="option.exampleNames && option.exampleNames.length > 0" class="mt-2 flex flex-wrap gap-1.5">
                <span
                  v-for="name in option.exampleNames"
                  :key="name"
                  class="inline-block px-2 py-0.5 text-xs rounded-full bg-[var(--color-cream)] text-[var(--color-warm-gray)]"
                >
                  {{ name }}
                </span>
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
          class="flex-1 py-3 px-4 rounded-xl font-semibold border-2 border-[var(--color-warm-gray-light)] text-[var(--color-warm-gray-light)] hover:bg-[var(--color-cream)] transition-colors"
        >
          Skip
        </button>
        <button
          @click="handleNext"
          :disabled="!selectedOption"
          class="flex-1 py-3 px-4 rounded-xl font-semibold bg-gradient-to-r from-[var(--color-sage)] to-[var(--color-sage-dark)] text-white shadow-md hover:shadow-lg transition-all disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {{ store.isLastQuestion ? 'Continue' : 'Next' }}
        </button>
      </div>
    </div>

    <!-- Loading state -->
    <div v-else-if="store.loading" class="text-center py-12">
      <div class="animate-spin w-8 h-8 border-4 border-[var(--color-sage)] border-t-transparent rounded-full mx-auto mb-4" />
      <p class="text-[var(--color-warm-gray-light)]">Loading questions...</p>
    </div>
  </div>
</template>

<style scoped>
.questionnaire {
  max-width: 500px;
  margin: 0 auto;
}

.filter-option-button {
  width: 100%;
  padding: 1rem 1.25rem;
  background: white;
  border: 2px solid var(--color-cream);
  border-radius: 1rem;
  transition: all 0.2s;
  text-align: left;
  color: var(--color-warm-gray);
}

.filter-option-button:hover {
  border-color: var(--color-sage-light, #a8c5a8);
  background: var(--color-cream);
}

.filter-option-button.selected {
  border-color: var(--color-sage-dark, #5a8a5a);
  background: linear-gradient(135deg, rgba(90, 138, 90, 0.1) 0%, rgba(168, 197, 168, 0.1) 100%);
}

.radio-circle {
  width: 1.5rem;
  height: 1.5rem;
  border-radius: 50%;
  border: 2px solid var(--color-warm-gray-light);
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  transition: all 0.2s;
}

.filter-option-button.selected .radio-circle {
  border-color: var(--color-sage-dark, #5a8a5a);
}

.radio-dot {
  width: 0.75rem;
  height: 0.75rem;
  border-radius: 50%;
  background: linear-gradient(135deg, var(--color-sage-dark, #5a8a5a) 0%, var(--color-sage, #7ab07a) 100%);
}
</style>
