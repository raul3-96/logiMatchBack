import { useNavigate, useParams } from 'react-router-dom'
import type { Booking } from '../types/booking.types'
import type { Trip } from '../../trips/types/trip.types'

function BookingDetailPage() {
  const { id } = useParams()
  const navigate = useNavigate()

  const storedBookings = localStorage.getItem('bookings')

  const bookings: Booking[] = storedBookings
    ? JSON.parse(storedBookings)
    : []

  const booking = bookings.find(
    (item) => item.id === id
  )

  if (!booking) {
    return (
      <div className="booking-detail-page">
        <h1>Reserva no encontrada</h1>

        <button
          type="button"
          onClick={() => navigate('/bookings')}
        >
          Volver a mis reservas
        </button>
      </div>
    )
  }

  return (
    <div className="booking-detail-page">
      <div className="page-header">
        <div>
          <h1>Detalle de reserva</h1>
          <p>Información de la reserva de transporte</p>
        </div>

        <button
          type="button"
          onClick={() => navigate('/bookings')}
        >
          Volver
        </button>
      </div>

      <div className="booking-detail-card">
        <div className="booking-detail-section">
          <h2>Ruta</h2>

          <div className="booking-detail-route">
            <strong>{booking.origin}</strong>
            <span>→</span>
            <strong>{booking.destination}</strong>
          </div>
        </div>

        <div className="booking-detail-section">
          <h2>Carga</h2>

          <div className="booking-detail-row">
            <span>Descripción</span>
            <strong>{booking.cargoDescription}</strong>
          </div>

          <div className="booking-detail-row">
            <span>Peso</span>
            <strong>{booking.weightKg} kg</strong>
          </div>

          <div className="booking-detail-row">
            <span>Volumen</span>
            <strong>{booking.volumeM3} m³</strong>
          </div>
        </div>

        <div className="booking-detail-section">
          <h2>Viaje</h2>

          <div className="booking-detail-row">
            <span>Vehículo</span>
            <strong>{booking.vehicle}</strong>
          </div>

          <div className="booking-detail-row">
            <span>ID del viaje</span>
            <strong>{booking.tripId}</strong>
          </div>
        </div>

        <div className="booking-detail-section">
          <h2>Reserva</h2>

          <div className="booking-detail-row">
            <span>Precio</span>
            <strong>{booking.price.toFixed(2)} €</strong>
          </div>

          <div className="booking-detail-row">
            <span>Estado</span>
            <span className="booking-detail-status">
              {booking.status}
            </span>
          </div>
        </div>
      </div>

      <button
        type="button"
        onClick={() => {
            const confirmed = window.confirm(
            '¿Estás seguro de que quieres cancelar esta reserva?'
            )

            if (!confirmed) {
            return
            }

            const storedBookings = localStorage.getItem('bookings')
            const currentBookings: Booking[] = storedBookings
            ? JSON.parse(storedBookings)
            : []

            const currentBooking = currentBookings.find(
            (item) => item.id === booking.id
            )
            
            if (!currentBooking) {
                alert('La reserva ya no existe')
                return
            }

            if (currentBooking.status === 'Cancelada') {
                alert('Esta reserva ya está cancelada')
                return
            }

            const storedTrips = localStorage.getItem('trips')
            const currentTrips: Trip[] = storedTrips
            ? JSON.parse(storedTrips)
            : []

            const currentTrip = currentTrips.find(
            (item) => item.id === currentBooking.tripId
            )

            if (!currentTrip) {
            alert('El viaje asociado ya no existe')
            return
            }

            const updatedTrip: Trip = {
            ...currentTrip,
            availableWeightKg:
                currentTrip.availableWeightKg +
                currentBooking.weightKg,
            availableVolumeM3:
                currentTrip.availableVolumeM3 +
                currentBooking.volumeM3,
            }

            const updatedTrips = currentTrips.map((item) =>
            item.id === currentTrip.id
                ? updatedTrip
                : item
            )

            localStorage.setItem(
            'trips',
            JSON.stringify(updatedTrips)
            )

            const updatedBookings = currentBookings.map((item) =>
            item.id === currentBooking.id
                ? {
                    ...item,
                    status: 'Cancelada',
                }
                : item
            )

            localStorage.setItem(
            'bookings',
            JSON.stringify(updatedBookings)
            )

            navigate('/bookings')
        }}
        >
        Cancelar reserva
        </button>
    </div>
  )
}

export default BookingDetailPage