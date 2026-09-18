import type { Booking } from '../types/booking.types'
import { useNavigate } from 'react-router-dom'

const mockBookings: Booking[] = [
  {
    id: '1',
    cargoDescription: 'Cajas de material',
    origin: 'Sevilla',
    destination: 'Madrid',
    tripId: '1',
    vehicle: 'Mercedes-Benz Sprinter',
    weightKg: 850,
    volumeM3: 4.5,
    price: 350,
    status: 'Confirmada',
  },
  {
    id: '2',
    cargoDescription: 'Mobiliario',
    origin: 'Sevilla',
    destination: 'Córdoba',
    tripId: '2',
    vehicle: 'Iveco Daily',
    weightKg: 300,
    volumeM3: 2,
    price: 180,
    status: 'Pendiente',
  },
]

function BookingsPage() {
  const navigate = useNavigate()
  const storedBookings = localStorage.getItem('bookings')

  const bookings: Booking[] = storedBookings
    ? JSON.parse(storedBookings)
    : mockBookings

  return (
    <div className="bookings-page">
      <div className="page-header">
        <div>
          <h1>Mis reservas</h1>
          <p>Gestiona tus reservas de transporte</p>
        </div>
      </div>

      <div className="booking-list">
        {bookings.map((booking) => (
          <div
            className="booking-card"
            key={booking.id}
            onClick={() => navigate(`/bookings/${booking.id}`)}
            >
            <div className="booking-route">
              <strong>{booking.origin}</strong>
              <span>→</span>
              <strong>{booking.destination}</strong>
            </div>

            <div className="booking-cargo">
              <strong>{booking.cargoDescription}</strong>
              <span>
                {booking.weightKg} kg · {booking.volumeM3} m³
              </span>
            </div>

            <div className="booking-vehicle">
              {booking.vehicle}
            </div>

            <div className="booking-price">
              {booking.price.toFixed(2)} €
            </div>

            <div className="booking-status">
              {booking.status}
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}

export default BookingsPage