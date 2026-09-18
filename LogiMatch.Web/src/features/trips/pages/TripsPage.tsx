import { useNavigate } from 'react-router-dom'
import type { Trip } from '../types/trip.types'

const mockTrips: Trip[] = [
  {
    id: '1',
    origin: 'Sevilla',
    destination: 'Madrid',
    vehicle: 'Mercedes-Benz Sprinter',
    departureDate: '2026-09-20T10:00',
    arrivalDate: '2026-09-20T17:00',
    availableWeightKg: 1000,
    availableVolumeM3: 5.5,
    status: 'Publicado',
  },
  {
    id: '2',
    origin: 'Madrid',
    destination: 'Valencia',
    vehicle: 'Iveco Daily',
    departureDate: '2026-09-22T09:00',
    arrivalDate: '2026-09-22T14:00',
    availableWeightKg: 750,
    availableVolumeM3: 4,
    status: 'Publicado',
  },
]

function TripsPage() {
  const navigate = useNavigate()

  const trips: Trip[] = (() => {
    const storedTrips = localStorage.getItem('trips')

    if (storedTrips) {
      return JSON.parse(storedTrips)
    }

    return mockTrips
  })()

  return (
    <div className="trips-page">
      <div className="page-header">
        <div>
          <h1>Mis viajes</h1>
          <p>Gestiona tus viajes publicados</p>
        </div>

        <button
          type="button"
          onClick={() => navigate('/trips/create')}
        >
          + Nuevo viaje
        </button>
      </div>

      <div className="trip-list">
        {trips.map((trip) => (
          <div
            className="trip-card"
            key={trip.id}
            onClick={() => navigate(`/trips/${trip.id}`)}
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
              <span>{trip.availableWeightKg} kg</span>
              <span>{trip.availableVolumeM3} m³</span>
            </div>

            <div className="trip-status">
              {trip.status}
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}

export default TripsPage