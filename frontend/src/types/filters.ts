export interface FilterOption {
  optionId: string
  label: string
  description: string | null
  exampleNames: string[] | null
  minValue: number | null
  maxValue: number | null
  allowedValues: string[] | null
}

export interface FilterQuestion {
  questionId: string
  questionText: string
  filterType: 'NAME_STYLE' | 'SYLLABLES' | 'ENDING_SOUND'
  options: FilterOption[]
}

export type NameStyle = 'none' | 'trendy' | 'classic' | 'unique'

export interface FilterAnswer {
  questionId: string
  selectedOptionId: string
}

export interface SubmitFiltersRequest {
  sessionId: string
  answers: FilterAnswer[]
}

export interface SessionFiltersStatus {
  sessionId: string
  initiatorCompleted: boolean
  partnerCompleted: boolean
  initiatorCompletedAt: string | null
  partnerCompletedAt: string | null
  bothCompleted: boolean
}

export interface UserFilters {
  nameStyle: number // 0=None, 1=Trendy, 2=Classic, 3=Unique
  minPopularityScore: number | null
  maxPopularityScore: number | null
  minSyllables: number | null
  maxSyllables: number | null
  allowedEndingSounds: string[] | null
  createdAt: string
}
