import { useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import type { Trip } from '../types/trip.types'

function TripDetailPage() {
  const { id } = useParams()
  const navigate = useNavigate()

  const [cargoDescription, setCargoDescription] = useState('')
  const [weightKg, setWeightKg] = useState('')
  const [volumeM3, setVolumeM3] = useState('')
  const [price, setPrice] = useState('')

  const [errors, setErrors] = useState<Record<string, string>>({})

  const storedTrips = localStorage.getItem('trips')

  const trips: Trip[] = storedTrips
    ? JSON.parse(storedTrips)
    : []

  const trip = trips.find((item) => item.id === id)

  if (!trip) {
    return (
      <div className="trip-detail-page">
        <h1>Viaje no encontrado</h1>

        <button
          type="button"
          onClick={() => navigate('/trips')}
        >
          Volver a mis viajes
        </button>
      </div>
    )
  }

  const clearError = (field: string) => {
    setErrors((currentErrors) => {
      const newErrors = { ...currentErrors }
      delete newErrors[field]
      return newErrors
    })
  }

  const handleReservation = (
    event: React.FormEvent<HTMLFormElement>,
  ) => {
    event.preventDefault()

    const newErrors: Record<string, string> = {}

    if (!cargoDescription.trim()) {
      newErrors.cargoDescription =
        'La descripción de la carga es obligatoria'
    }

    if (!weightKg) {
      newErrors.weightKg =
        'El peso es obligatorio'
    } else if (Number(weightKg) <= 0) {
      newErrors.weightKg =
        'El peso debe ser mayor que 0'
    } else if (Number(weightKg) > trip.availableWeightKg) {
      newErrors.weightKg =
        'El peso supera la capacidad disponible'
    }

    if (!volumeM3) {
      newErrors.volumeM3 =
        'El volumen es obligatorio'
    } else if (Number(volumeM3) <= 0) {
      newErrors.volumeM3 =
        'El volumen debe ser mayor que 0'
    } else if (Number(volumeM3) > trip.availableVolumeM3) {
      newErrors.volumeM3 =
        'El volumen supera la capacidad disponible'
    }

    if (!price) {
      newErrors.price =
        'El precio es obligatorio'
    } else if (Number(price) <= 0) {
      newErrors.price =
        'El precio debe ser mayor que 0'
    }

    setErrors(newErrors)

    if (Object.keys(newErrors).length > 0) {
      return
    }

    const currentStoredTrips = localStorage.getItem('trips')

    const currentTrips: Trip[] = currentStoredTrips
    ? JSON.parse(currentStoredTrips)
    : []

    const currentTrip = currentTrips.find(
    (item) => item.id === trip.id,
    )

    if (!currentTrip) {
    setErrors({
        reservation: 'El viaje ya no está disponible',
    })

    return
    }

    if (
    Number(weightKg) > currentTrip.availableWeightKg
    ) {
    setErrors({
        weightKg:
        'La capacidad de peso disponible ha cambiado',
    })

    return
    }

    if (
    Number(volumeM3) > currentTrip.availableVolumeM3
    ) {
    setErrors({
        volumeM3:
        'La capacidad de volumen disponible ha cambiado',
    })

    return
    }
    //New Bookings
    const storedBookings = localStorage.getItem('bookings')

    const bookings = storedBookings
    ? JSON.parse(storedBookings)
    : []

    const newBooking = {
    id: crypto.randomUUID(),
    cargoDescription: cargoDescription.trim(),
    origin: trip.origin,
    destination: trip.destination,
    tripId: trip.id,
    vehicle: trip.vehicle,
    weightKg: Number(weightKg),
    volumeM3: Number(volumeM3),
    price: Number(price),
    status: 'Confirmada',
    }

    bookings.push(newBooking)

    localStorage.setItem(
    'bookings',
    JSON.stringify(bookings),
    )

    const updatedTrip: Trip = {
    ...currentTrip,
    availableWeightKg:
        trip.availableWeightKg - Number(weightKg),
    availableVolumeM3:
        trip.availableVolumeM3 - Number(volumeM3),
    }

    const updatedTrips = trips.map((item) =>
    item.id === trip.id
        ? updatedTrip
        : item,
    )

    localStorage.setItem(
    'trips',
    JSON.stringify(updatedTrips),
    )

    navigate('/bookings')
  }

  return (
    <div className="trip-detail-page">
      <div className="page-header">
        <div>
          <h1>Detalle del viaje</h1>
          <p>Información del viaje publicado</p>
        </div>

        <button
          type="button"
          onClick={() => navigate('/trips')}
        >
          Volver
        </button>
      </div>

      <div className="trip-detail-card">
        <section>
          <h2>Ruta</h2>

          <div className="trip-detail-route">
            <strong>{trip.origin}</strong>
            <span>→</span>
            <strong>{trip.destination}</strong>
          </div>
        </section>

        <section>
          <h2>Vehículo</h2>

          <p>{trip.vehicle}</p>
        </section>

        <section>
          <h2>Horario</h2>

          <div className="trip-detail-data">
            <span>
              Salida: {trip.departureDate.replace('T', ' ')}
            </span>

            <span>
              Llegada: {trip.arrivalDate.replace('T', ' ')}
            </span>
          </div>
        </section>

        <section>
          <h2>Capacidad disponible</h2>

          <div className="trip-detail-capacity">
            <span>{trip.availableWeightKg} kg</span>
            <span>{trip.availableVolumeM3} m³</span>
          </div>
        </section>

        <section>
          <h2>Estado</h2>

          <span className="trip-status">
            {trip.status}
          </span>
        </section>

        <section className="reservation-section">
          <h2>Reservar capacidad</h2>

          <form onSubmit={handleReservation}>
            <div className="form-field">
              <label htmlFor="cargoDescription">
                Descripción de la carga
              </label>

              <input
                id="cargoDescription"
                type="text"
                value={cargoDescription}
                onChange={(event) => {
                  setCargoDescription(event.target.value)
                  clearError('cargoDescription')
                }}
                placeholder="Ej. Cajas de material"
              />

              {errors.cargoDescription && (
                <span className="form-error">
                  {errors.cargoDescription}
                </span>
              )}
            </div>

            <div className="form-grid">
              <div className="form-field">
                <label htmlFor="weightKg">
                  Peso (kg)
                </label>

                <input
                  id="weightKg"
                  type="number"
                  min="0"
                  step="0.01"
                  value={weightKg}
                  onChange={(event) => {
                    setWeightKg(event.target.value)
                    clearError('weightKg')
                  }}
                  placeholder="850"
                />

                {errors.weightKg && (
                  <span className="form-error">
                    {errors.weightKg}
                  </span>
                )}
              </div>

              <div className="form-field">
                <label htmlFor="volumeM3">
                  Volumen (m³)
                </label>

                <input
                  id="volumeM3"
                  type="number"
                  min="0"
                  step="0.1"
                  value={volumeM3}
                  onChange={(event) => {
                    setVolumeM3(event.target.value)
                    clearError('volumeM3')
                  }}
                  placeholder="4.5"
                />

                {errors.volumeM3 && (
                  <span className="form-error">
                    {errors.volumeM3}
                  </span>
                )}
              </div>
            </div>

            <div className="form-field">
              <label htmlFor="price">
                Precio (€)
              </label>

              <input
                id="price"
                type="number"
                min="0"
                step="0.01"
                value={price}
                onChange={(event) => {
                  setPrice(event.target.value)
                  clearError('price')
                }}
                placeholder="350"
              />

              {errors.price && (
                <span className="form-error">
                  {errors.price}
                </span>
              )}
            </div>

            <button type="submit">
              Comprobar reserva
            </button>
          </form>
        </section>
      </div>
    </div>
  )
}

export default TripDetailPage