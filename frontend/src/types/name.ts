import { Gender } from './session'

export interface Name {
  id: number
  nameText: string
  gender: Gender
  popularityScore: number
  origin: string | null
}
