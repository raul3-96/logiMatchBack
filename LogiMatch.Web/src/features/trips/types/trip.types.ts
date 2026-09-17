export interface Trip {
  id: string
  origin: string
  destination: string
  vehicle: string
  departureDate: string
  arrivalDate: string
  availableWeightKg: number
  availableVolumeM3: number
  status: string
}