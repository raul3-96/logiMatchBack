import { useNavigate, useParams } from 'react-router-dom'
import type { TransportRequest } from '../types/transportRequest.types'
import type { Trip } from '../../trips/types/trip.types'

function MatchingTripsPage() {
  const { id } = useParams()
  const navigate = useNavigate()

  const storedRequests = localStorage.getItem('transportRequests')
  const requests: TransportRequest[] = storedRequests
    ? JSON.parse(storedRequests)
    : []

  const request = requests.find((item) => item.id === id)

  if (!request) {
    return (
      <div>
        <h1>Carga no encontrada</h1>

        <button
          type="button"
          onClick={() => navigate('/transport-requests')}
        >
          Volver a mis cargas
        </button>
      </div>
    )
  }

  const storedTrips = localStorage.getItem('trips')
  const trips: Trip[] = storedTrips
    ? JSON.parse(storedTrips)
    : []

  const matchingTrips = trips.filter((trip) => {
    const enoughWeight =
      trip.availableWeightKg >= request.weightKg

    const enoughVolume =
      trip.availableVolumeM3 >= request.volumeM3

    const tripDepartureDate =
        trip.departureDate.split('T')[0]

    const tripArrivalDate =
        trip.arrivalDate.split('T')[0]

    const compatibleDates =
        tripDepartureDate >= request.pickupDate &&
        tripArrivalDate <= request.deliveryDate
    
    //TODO: A tener en cuenta la posible localizacion
    /*const compatibleRoute =
        trip.origin.trim().toLowerCase() === request.origin.trim().toLowerCase() &&
        trip.destination.trim().toLowerCase() === request.destination.trim().toLowerCase()*/

    return (
      enoughWeight &&
      enoughVolume &&
      compatibleDates /*&&
      compatibleRoute*/
    )
  })

  return (
    <div className="matching-trips-page">
      <div className="page-header">
        <div>
          <h1>Viajes compatibles</h1>
          <p>
            Viajes disponibles para transportar esta carga
          </p>
        </div>

        <button
          type="button"
          onClick={() => navigate('/transport-requests')}
        >
          Volver
        </button>
      </div>

      <div className="matching-request-summary">
        <strong>
          {request.origin} → {request.destination}
        </strong>

        <span>{request.cargoDescription}</span>

        <span>
          {request.weightKg} kg · {request.volumeM3} m³
        </span>
      </div>

      {matchingTrips.length === 0 ? (
        <div className="empty-state">
          <h2>No hay viajes compatibles</h2>
          <p>
            Actualmente no hay viajes con capacidad y fechas
            compatibles con esta carga.
          </p>
        </div>
      ) : (
        <div className="matching-trip-list">
          {matchingTrips.map((trip) => (
            <div
              className="matching-trip-card"
              key={trip.id}
            >
              <div className="trip-route">
                <strong>{trip.origin}</strong>
                <span>→</span>
                <strong>{trip.destination}</strong>
              </div>

              <div className="trip-vehicle">
                {trip.vehicle}
              </div>

              <div className="trip-dates">
                <span>
                  Salida: {trip.departureDate.replace('T', ' ')}
                </span>

                <span>
                  Llegada: {trip.arrivalDate.replace('T', ' ')}
                </span>
              </div>

              <div className="trip-capacity">
                <span>
                  {trip.availableWeightKg} kg disponibles
                </span>

                <span>
                  {trip.availableVolumeM3} m³ disponibles
                </span>
              </div>

              <button
                type="button"
                onClick={() => {
                    const storedTrips = localStorage.getItem('trips')
                    const currentTrips: Trip[] = storedTrips
                    ? JSON.parse(storedTrips)
                    : []

                    const currentTrip = currentTrips.find(
                    (item) => item.id === trip.id
                    )

                    if (!currentTrip) {
                    alert('El viaje ya no está disponible')
                    return
                    }

                    const storedBookings = localStorage.getItem('bookings')
                    const bookings = storedBookings
                    ? JSON.parse(storedBookings)
                    : []

                    const alreadyBooked = bookings.some(
                    (booking: {
                        tripId: string
                        cargoDescription: string
                    }) =>
                        booking.tripId === currentTrip.id &&
                        booking.cargoDescription === request.cargoDescription
                    )

                    if (alreadyBooked) {
                    alert('Esta carga ya está reservada en este viaje')
                    return
                    }

                    if (
                    currentTrip.availableWeightKg < request.weightKg ||
                    currentTrip.availableVolumeM3 < request.volumeM3
                    ) {
                    alert('La capacidad disponible ha cambiado')
                    return
                    }

                    const newBooking = {
                    id: crypto.randomUUID(),
                    cargoDescription: request.cargoDescription,
                    origin: request.origin,
                    destination: request.destination,
                    tripId: currentTrip.id,
                    vehicle: currentTrip.vehicle,
                    weightKg: request.weightKg,
                    volumeM3: request.volumeM3,
                    price: 0,
                    status: 'Confirmada',
                    }

                    bookings.push(newBooking)

                    localStorage.setItem(
                    'bookings',
                    JSON.stringify(bookings)
                    )

                    const updatedTrip: Trip = {
                    ...currentTrip,
                    availableWeightKg:
                        currentTrip.availableWeightKg - request.weightKg,
                    availableVolumeM3:
                        currentTrip.availableVolumeM3 - request.volumeM3,
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

                    navigate('/bookings')
                }}
                >
                Reservar este viaje
                </button>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}

export default MatchingTripsPage