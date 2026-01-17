import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type {
  Category,
  PreferenceQuestion,
  PreferenceAnswer,
  SubmitPreferencesRequest,
  UserPreference,
  SessionPreferencesStatus,
} from '@/types/preferences'
import api from '@/services/api'

export const usePreferencesStore = defineStore('preferences', () => {
  // State
  const categories = ref<Category[]>([])
  const questions = ref<PreferenceQuestion[]>([])
  const answers = ref<Map<string, string[]>>(new Map())
  const userPreferences = ref<UserPreference[]>([])
  const status = ref<SessionPreferencesStatus | null>(null)
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
  const hasCompletedPreferences = computed(() => status.value?.initiatorCompleted || status.value?.partnerCompleted)
  const canStartVoting = computed(() => status.value?.canStartVoting ?? false)

  // Actions
  async function fetchCategories(type?: string) {
    loading.value = true
    error.value = null
    try {
      const params = type ? { type } : {}
      const response = await api.get('/preferences/categories', { params })
      categories.value = response.data.data || []
    } catch (e: any) {
      error.value = e.response?.data?.errors?.[0] || 'Failed to fetch categories'
    } finally {
      loading.value = false
    }
  }

  async function fetchQuestions() {
    loading.value = true
    error.value = null
    try {
      const response = await api.get('/preferences/questions')
      questions.value = response.data.data || []
      currentQuestionIndex.value = 0
    } catch (e: any) {
      error.value = e.response?.data?.errors?.[0] || 'Failed to fetch questions'
    } finally {
      loading.value = false
    }
  }

  async function fetchStatus() {
    try {
      const response = await api.get('/preferences/status')
      status.value = response.data.data || null
    } catch (e: any) {
      // 404 means no active session, which is okay
      if (e.response?.status !== 404) {
        error.value = e.response?.data?.errors?.[0] || 'Failed to fetch status'
      }
      status.value = null
    }
  }

  async function fetchUserPreferences() {
    try {
      const response = await api.get('/preferences/mine')
      userPreferences.value = response.data.data || []
    } catch (e: any) {
      error.value = e.response?.data?.errors?.[0] || 'Failed to fetch preferences'
    }
  }

  function setAnswer(questionId: string, optionIds: string[]) {
    answers.value.set(questionId, optionIds)
  }

  function getAnswer(questionId: string): string[] {
    return answers.value.get(questionId) || []
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

  async function submitPreferences(): Promise<SessionPreferencesStatus | null> {
    submitting.value = true
    error.value = null
    try {
      // Convert answers map to array format
      const answersArray: PreferenceAnswer[] = []
      answers.value.forEach((optionIds, questionId) => {
        if (optionIds.length > 0) {
          answersArray.push({ questionId, selectedOptionIds: optionIds })
        }
      })

      const request: SubmitPreferencesRequest = { answers: answersArray }
      const response = await api.post('/preferences', request)
      status.value = response.data.data
      return status.value
    } catch (e: any) {
      error.value = e.response?.data?.errors?.[0] || 'Failed to submit preferences'
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
    categories,
    questions,
    answers,
    userPreferences,
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
    hasCompletedPreferences,
    canStartVoting,
    // Actions
    fetchCategories,
    fetchQuestions,
    fetchStatus,
    fetchUserPreferences,
    setAnswer,
    getAnswer,
    nextQuestion,
    previousQuestion,
    goToQuestion,
    submitPreferences,
    resetQuestionnaire,
    clearError,
  }
})
