export const PreferenceLevel = {
  Avoid: -2,
  Dislike: -1,
  Neutral: 0,
  Like: 1,
  Love: 2,
} as const

export type PreferenceLevel = (typeof PreferenceLevel)[keyof typeof PreferenceLevel]

export const SessionSetupStatus = {
  PendingInitiatorPreferences: 0,
  PendingPartnerPreferences: 1,
  Ready: 2,
} as const

export type SessionSetupStatus = (typeof SessionSetupStatus)[keyof typeof SessionSetupStatus]

export interface Category {
  id: number
  code: string
  displayName: string
  categoryType: 'ORIGIN' | 'STYLE' | 'SOUND'
  description: string | null
}

export interface PreferenceOption {
  optionId: string
  label: string
  description: string | null
  categoryCodes: string[]
  preferenceLevel: PreferenceLevel
}

export interface PreferenceQuestion {
  questionId: string
  questionText: string
  categoryType: string
  allowMultiple: boolean
  options: PreferenceOption[]
}

export interface PreferenceAnswer {
  questionId: string
  selectedOptionIds: string[]
}

export interface SubmitPreferencesRequest {
  answers: PreferenceAnswer[]
}

export interface UserPreference {
  id: number
  categoryId: number
  categoryCode: string
  categoryName: string
  categoryType: string
  level: PreferenceLevel
}

export interface SessionPreferencesStatus {
  sessionId: string
  setupStatus: SessionSetupStatus
  initiatorCompleted: boolean
  partnerCompleted: boolean
  initiatorCompletedAt: string | null
  partnerCompletedAt: string | null
  bothCompleted: boolean
  canStartVoting: boolean
}
