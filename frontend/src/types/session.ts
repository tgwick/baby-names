export const Gender = {
  Male: 0,
  Female: 1,
  Neutral: 2,
} as const

export type Gender = (typeof Gender)[keyof typeof Gender]

export const SessionStatus = {
  WaitingForPartner: 0,
  Active: 1,
  Completed: 2,
} as const

export type SessionStatus = (typeof SessionStatus)[keyof typeof SessionStatus]

export interface Session {
  id: string
  initiatorId: string
  partnerId: string | null
  targetGender: Gender
  joinCode: string
  partnerLink: string
  status: SessionStatus
  createdAt: string
  linkedAt: string | null
  isInitiator: boolean
  partnerDisplayName: string | null
  initiatorDisplayName: string | null
}

export interface CreateSessionRequest {
  targetGender: Gender
}

export interface JoinSessionRequest {
  joinCode: string
}
