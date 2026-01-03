import { Gender } from './session'

export const VoteType = {
  Like: 0,
  Dislike: 1,
} as const

export type VoteType = (typeof VoteType)[keyof typeof VoteType]

export interface SubmitVoteRequest {
  nameId: number
  voteType: VoteType
}

export interface VoteResult {
  voteId: number
  isMatch: boolean
  match: Match | null
}

export interface Vote {
  id: number
  nameId: number
  nameText: string
  voteType: VoteType
  votedAt: string
}

export interface Match {
  nameId: number
  nameText: string
  gender: Gender
  origin: string | null
  popularityScore: number
  matchedAt: string
}

export interface VoteStats {
  totalVotes: number
  likeCount: number
  dislikeCount: number
  matchCount: number
  namesRemaining: number
}
