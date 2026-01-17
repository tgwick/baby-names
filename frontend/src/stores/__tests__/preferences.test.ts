import { describe, it, expect, beforeEach, vi } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { usePreferencesStore } from '../preferences'
import api from '@/services/api'

vi.mock('@/services/api')

const mockQuestions = [
  {
    questionId: 'style',
    questionText: 'What name styles appeal to you most?',
    categoryType: 'STYLE',
    allowMultiple: true,
    options: [
      { optionId: 'classic', label: 'Classic', description: 'Timeless', categoryCodes: ['CLASSIC'], preferenceLevel: 2 },
      { optionId: 'modern', label: 'Modern', description: 'Fresh', categoryCodes: ['MODERN'], preferenceLevel: 2 },
      { optionId: 'no_pref', label: 'No preference', description: '', categoryCodes: [], preferenceLevel: 0 },
    ],
  },
  {
    questionId: 'origin',
    questionText: 'Do you have cultural preferences?',
    categoryType: 'ORIGIN',
    allowMultiple: true,
    options: [
      { optionId: 'hebrew', label: 'Hebrew', description: '', categoryCodes: ['HEBREW'], preferenceLevel: 2 },
      { optionId: 'no_pref', label: 'No preference', description: '', categoryCodes: [], preferenceLevel: 0 },
    ],
  },
]

const mockCategories = [
  { id: '1', code: 'CLASSIC', displayName: 'Classic', categoryType: 'STYLE', description: '', displayOrder: 1 },
  { id: '2', code: 'MODERN', displayName: 'Modern', categoryType: 'STYLE', description: '', displayOrder: 2 },
]

describe('Preferences Store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
  })

  describe('Initial State', () => {
    it('should have empty initial state', () => {
      const store = usePreferencesStore()
      expect(store.categories).toEqual([])
      expect(store.questions).toEqual([])
      expect(store.userPreferences).toEqual([])
      expect(store.status).toBeNull()
      expect(store.loading).toBe(false)
      expect(store.submitting).toBe(false)
      expect(store.error).toBeNull()
      expect(store.currentQuestionIndex).toBe(0)
    })
  })

  describe('fetchQuestions', () => {
    it('should fetch questions and set them', async () => {
      const store = usePreferencesStore()
      vi.mocked(api.get).mockResolvedValueOnce({ data: { data: mockQuestions } })

      await store.fetchQuestions()

      expect(api.get).toHaveBeenCalledWith('/preferences/questions')
      expect(store.questions).toEqual(mockQuestions)
      expect(store.loading).toBe(false)
    })

    it('should handle fetch error', async () => {
      const store = usePreferencesStore()
      vi.mocked(api.get).mockRejectedValueOnce({
        response: { data: { errors: ['Failed to load'] } },
      })

      await store.fetchQuestions()

      expect(store.error).toBe('Failed to load')
      expect(store.questions).toEqual([])
    })
  })

  describe('fetchCategories', () => {
    it('should fetch categories', async () => {
      const store = usePreferencesStore()
      vi.mocked(api.get).mockResolvedValueOnce({ data: { data: mockCategories } })

      await store.fetchCategories()

      expect(api.get).toHaveBeenCalledWith('/preferences/categories', { params: {} })
      expect(store.categories).toEqual(mockCategories)
    })

    it('should fetch categories with type filter', async () => {
      const store = usePreferencesStore()
      vi.mocked(api.get).mockResolvedValueOnce({ data: { data: mockCategories } })

      await store.fetchCategories('STYLE')

      expect(api.get).toHaveBeenCalledWith('/preferences/categories', { params: { type: 'STYLE' } })
    })
  })

  describe('Question Navigation', () => {
    it('should track current question', async () => {
      const store = usePreferencesStore()
      vi.mocked(api.get).mockResolvedValueOnce({ data: { data: mockQuestions } })
      await store.fetchQuestions()

      expect(store.currentQuestion).toEqual(mockQuestions[0])
      expect(store.currentQuestionIndex).toBe(0)
      expect(store.isFirstQuestion).toBe(true)
      expect(store.isLastQuestion).toBe(false)
    })

    it('should navigate to next question', async () => {
      const store = usePreferencesStore()
      vi.mocked(api.get).mockResolvedValueOnce({ data: { data: mockQuestions } })
      await store.fetchQuestions()

      store.nextQuestion()

      expect(store.currentQuestionIndex).toBe(1)
      expect(store.currentQuestion).toEqual(mockQuestions[1])
      expect(store.isFirstQuestion).toBe(false)
      expect(store.isLastQuestion).toBe(true)
    })

    it('should navigate to previous question', async () => {
      const store = usePreferencesStore()
      vi.mocked(api.get).mockResolvedValueOnce({ data: { data: mockQuestions } })
      await store.fetchQuestions()
      store.nextQuestion()

      store.previousQuestion()

      expect(store.currentQuestionIndex).toBe(0)
    })

    it('should not go below 0', async () => {
      const store = usePreferencesStore()
      vi.mocked(api.get).mockResolvedValueOnce({ data: { data: mockQuestions } })
      await store.fetchQuestions()

      store.previousQuestion()

      expect(store.currentQuestionIndex).toBe(0)
    })

    it('should not go past last question', async () => {
      const store = usePreferencesStore()
      vi.mocked(api.get).mockResolvedValueOnce({ data: { data: mockQuestions } })
      await store.fetchQuestions()
      store.nextQuestion()

      store.nextQuestion()

      expect(store.currentQuestionIndex).toBe(1)
    })

    it('should go to specific question', async () => {
      const store = usePreferencesStore()
      vi.mocked(api.get).mockResolvedValueOnce({ data: { data: mockQuestions } })
      await store.fetchQuestions()

      store.goToQuestion(1)

      expect(store.currentQuestionIndex).toBe(1)
    })
  })

  describe('Progress Calculation', () => {
    it('should calculate progress correctly', async () => {
      const store = usePreferencesStore()
      vi.mocked(api.get).mockResolvedValueOnce({ data: { data: mockQuestions } })
      await store.fetchQuestions()

      expect(store.progress).toBe(50) // 1 of 2 questions = 50%

      store.nextQuestion()
      expect(store.progress).toBe(100) // 2 of 2 questions = 100%
    })

    it('should handle zero questions', () => {
      const store = usePreferencesStore()
      expect(store.progress).toBe(0)
    })
  })

  describe('Answers', () => {
    it('should set and get answers', () => {
      const store = usePreferencesStore()

      store.setAnswer('style', ['classic', 'modern'])

      expect(store.getAnswer('style')).toEqual(['classic', 'modern'])
    })

    it('should return empty array for unanswered question', () => {
      const store = usePreferencesStore()

      expect(store.getAnswer('nonexistent')).toEqual([])
    })

    it('should reset questionnaire', async () => {
      const store = usePreferencesStore()
      vi.mocked(api.get).mockResolvedValueOnce({ data: { data: mockQuestions } })
      await store.fetchQuestions()
      store.setAnswer('style', ['classic'])
      store.nextQuestion()

      store.resetQuestionnaire()

      expect(store.getAnswer('style')).toEqual([])
      expect(store.currentQuestionIndex).toBe(0)
    })
  })

  describe('submitPreferences', () => {
    it('should submit preferences successfully', async () => {
      const store = usePreferencesStore()
      const mockResponse = {
        sessionId: '123',
        setupStatus: 1,
        initiatorCompleted: true,
        partnerCompleted: false,
        bothCompleted: false,
        canStartVoting: false,
      }
      vi.mocked(api.post).mockResolvedValueOnce({ data: { data: mockResponse } })

      store.setAnswer('style', ['classic', 'modern'])
      store.setAnswer('origin', ['hebrew'])

      const result = await store.submitPreferences()

      expect(api.post).toHaveBeenCalledWith('/preferences', {
        answers: [
          { questionId: 'style', selectedOptionIds: ['classic', 'modern'] },
          { questionId: 'origin', selectedOptionIds: ['hebrew'] },
        ],
      })
      expect(result).toEqual(mockResponse)
      expect(store.status).toEqual(mockResponse)
    })

    it('should not include empty answers', async () => {
      const store = usePreferencesStore()
      vi.mocked(api.post).mockResolvedValueOnce({ data: { data: {} } })

      store.setAnswer('style', ['classic'])
      store.setAnswer('origin', []) // Empty answer

      await store.submitPreferences()

      expect(api.post).toHaveBeenCalledWith('/preferences', {
        answers: [{ questionId: 'style', selectedOptionIds: ['classic'] }],
      })
    })

    it('should handle submit error', async () => {
      const store = usePreferencesStore()
      vi.mocked(api.post).mockRejectedValueOnce({
        response: { data: { errors: ['No active session'] } },
      })

      store.setAnswer('style', ['classic'])

      await expect(store.submitPreferences()).rejects.toBeDefined()
      expect(store.error).toBe('No active session')
    })
  })

  describe('fetchStatus', () => {
    it('should fetch status successfully', async () => {
      const store = usePreferencesStore()
      const mockStatus = {
        sessionId: '123',
        setupStatus: 2,
        initiatorCompleted: true,
        partnerCompleted: true,
        bothCompleted: true,
        canStartVoting: true,
      }
      vi.mocked(api.get).mockResolvedValueOnce({ data: { data: mockStatus } })

      await store.fetchStatus()

      expect(api.get).toHaveBeenCalledWith('/preferences/status')
      expect(store.status).toEqual(mockStatus)
      expect(store.canStartVoting).toBe(true)
    })

    it('should handle 404 gracefully', async () => {
      const store = usePreferencesStore()
      vi.mocked(api.get).mockRejectedValueOnce({ response: { status: 404 } })

      await store.fetchStatus()

      expect(store.status).toBeNull()
      expect(store.error).toBeNull()
    })
  })
})
