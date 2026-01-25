import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type {
  FilterQuestion,
  FilterAnswer,
  SubmitFiltersRequest,
  UserFilters,
  SessionFiltersStatus,
} from '@/types/filters'
import api from '@/services/api'

export const useFiltersStore = defineStore('filters', () => {
  // State
  const questions = ref<FilterQuestion[]>([])
  const answers = ref<Map<string, string>>(new Map())
  const userFilters = ref<UserFilters | null>(null)
  const status = ref<SessionFiltersStatus | null>(null)
  const loading = ref(false)
  const submitting = ref(false)
  const error = ref<string | null>(null)

  // Current question tracking for wizard
  const currentQuestionIndex = ref(0)

  // Computed
  const currentQuestion = computed(() => questions.value[currentQuestionIndex.value] || null)
  const totalQuestions = computed(() => questions.value.length)
  const isFirstQuestion = computed(() => currentQuestionIndex.value === 0)
  const isLastQuestion = computed(() => currentQuestionIndex.value === questions.value.length - 1)
  const progress = computed(() => {
    if (totalQuestions.value === 0) return 0
    return ((currentQuestionIndex.value + 1) / totalQuestions.value) * 100
  })
  const hasCompletedFilters = computed(() => {
    if (!status.value) return false
    return status.value.initiatorCompleted || status.value.partnerCompleted
  })

  // Actions
  async function fetchQuestions() {
    loading.value = true
    error.value = null
    try {
      const response = await api.get('/filters/questions')
      questions.value = response.data.data || []
      currentQuestionIndex.value = 0
    } catch (e: unknown) {
      const err = e as { response?: { data?: { errors?: string[] } } }
      error.value = err.response?.data?.errors?.[0] || 'Failed to fetch filter questions'
    } finally {
      loading.value = false
    }
  }

  async function fetchStatus() {
    try {
      const response = await api.get('/filters/status')
      status.value = response.data.data || null
    } catch (e: unknown) {
      const err = e as { response?: { status?: number; data?: { errors?: string[] } } }
      // 404 means no active session, which is okay
      if (err.response?.status !== 404) {
        error.value = err.response?.data?.errors?.[0] || 'Failed to fetch filter status'
      }
      status.value = null
    }
  }

  async function fetchUserFilters() {
    try {
      const response = await api.get('/filters/mine')
      userFilters.value = response.data.data || null
    } catch (e: unknown) {
      const err = e as { response?: { status?: number; data?: { errors?: string[] } } }
      // 404 means no filters set, which is okay
      if (err.response?.status !== 404) {
        error.value = err.response?.data?.errors?.[0] || 'Failed to fetch filters'
      }
      userFilters.value = null
    }
  }

  function setAnswer(questionId: string, optionId: string) {
    answers.value.set(questionId, optionId)
  }

  function getAnswer(questionId: string): string | undefined {
    return answers.value.get(questionId)
  }

  function nextQuestion() {
    if (currentQuestionIndex.value < questions.value.length - 1) {
      currentQuestionIndex.value++
    }
  }

  function previousQuestion() {
    if (currentQuestionIndex.value > 0) {
      currentQuestionIndex.value--
    }
  }

  function goToQuestion(index: number) {
    if (index >= 0 && index < questions.value.length) {
      currentQuestionIndex.value = index
    }
  }

  async function submitFilters(): Promise<SessionFiltersStatus | null> {
    submitting.value = true
    error.value = null
    try {
      // Convert answers map to array format
      const answersArray: FilterAnswer[] = []
      answers.value.forEach((optionId, questionId) => {
        answersArray.push({ questionId, selectedOptionId: optionId })
      })

      const request: SubmitFiltersRequest = { answers: answersArray }
      const response = await api.post('/filters', request)
      status.value = response.data.data
      return status.value
    } catch (e: unknown) {
      const err = e as { response?: { data?: { errors?: string[] } } }
      error.value = err.response?.data?.errors?.[0] || 'Failed to submit filters'
      throw e
    } finally {
      submitting.value = false
    }
  }

  function resetQuestionnaire() {
    answers.value = new Map()
    currentQuestionIndex.value = 0
    error.value = null
  }

  function clearError() {
    error.value = null
  }

  return {
    // State
    questions,
    answers,
    userFilters,
    status,
    loading,
    submitting,
    error,
    currentQuestionIndex,
    // Computed
    currentQuestion,
    totalQuestions,
    isFirstQuestion,
    isLastQuestion,
    progress,
    hasCompletedFilters,
    // Actions
    fetchQuestions,
    fetchStatus,
    fetchUserFilters,
    setAnswer,
    getAnswer,
    nextQuestion,
    previousQuestion,
    goToQuestion,
    submitFilters,
    resetQuestionnaire,
    clearError,
  }
})
