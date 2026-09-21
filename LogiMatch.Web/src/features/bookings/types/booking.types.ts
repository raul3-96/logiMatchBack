export interface Booking {
  id: string
  cargoDescription: string
  origin: string
  destination: string
  tripId: string
  vehicle: string
  weightKg: number
  volumeM3: number
  price: number
  status: string
}