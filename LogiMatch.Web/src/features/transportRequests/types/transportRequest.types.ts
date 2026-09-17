export interface TransportRequest {
  id: string
  origin: string
  destination: string
  cargoDescription: string
  weightKg: number
  volumeM3: number
  status: string
  pickupDate: string
  deliveryDate: string
}