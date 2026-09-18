import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import type { Trip } from '../types/trip.types'

function CreateTripPage() {
  const navigate = useNavigate()

  const [origin, setOrigin] = useState('')
  const [destination, setDestination] = useState('')
  const [vehicle, setVehicle] = useState('')
  const [departureDate, setDepartureDate] = useState('')
  const [arrivalDate, setArrivalDate] = useState('')
  const [availableWeightKg, setAvailableWeightKg] = useState('')
  const [availableVolumeM3, setAvailableVolumeM3] = useState('')

  const [errors, setErrors] = useState<Record<string, string>>({})

  const clearError = (field: string) => {
    setErrors((currentErrors) => {
      const newErrors = { ...currentErrors }
      delete newErrors[field]
      return newErrors
    })
  }

  const handleSubmit = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()

    const newErrors: Record<string, string> = {}

    if (!origin.trim()) {
      newErrors.origin = 'El origen es obligatorio'
    }

    if (!destination.trim()) {
      newErrors.destination = 'El destino es obligatorio'
    }

    if (!vehicle.trim()) {
      newErrors.vehicle = 'El vehículo es obligatorio'
    }

    if (!departureDate) {
      newErrors.departureDate = 'La fecha de salida es obligatoria'
    }

    if (!arrivalDate) {
      newErrors.arrivalDate = 'La fecha de llegada es obligatoria'
    } else if (departureDate && arrivalDate < departureDate) {
      newErrors.arrivalDate =
        'La llegada no puede ser anterior a la salida'
    }

    if (!availableWeightKg) {
      newErrors.availableWeightKg =
        'La capacidad de peso es obligatoria'
    } else if (Number(availableWeightKg) <= 0) {
      newErrors.availableWeightKg =
        'La capacidad debe ser mayor que 0'
    }

    if (!availableVolumeM3) {
      newErrors.availableVolumeM3 =
        'La capacidad de volumen es obligatoria'
    } else if (Number(availableVolumeM3) <= 0) {
      newErrors.availableVolumeM3 =
        'La capacidad debe ser mayor que 0'
    }

    setErrors(newErrors)

    if (Object.keys(newErrors).length > 0) {
      return
    }

    const newTrip: Trip = {
      id: crypto.randomUUID(),
      origin: origin.trim(),
      destination: destination.trim(),
      vehicle: vehicle.trim(),
      departureDate,
      arrivalDate,
      availableWeightKg: Number(availableWeightKg),
      availableVolumeM3: Number(availableVolumeM3),
      status: 'Publicado',
    }

    const storedTrips = localStorage.getItem('trips')

    const trips: Trip[] = storedTrips
      ? JSON.parse(storedTrips)
      : []

    trips.push(newTrip)

    localStorage.setItem(
      'trips',
      JSON.stringify(trips),
    )

    navigate('/trips')
  }

  return (
    <div className="create-trip-page">
      <div className="page-header">
        <div>
          <h1>Nuevo viaje</h1>
          <p>Crea un nuevo viaje para transportar cargas</p>
        </div>
      </div>

      <form
        className="trip-form"
        onSubmit={handleSubmit}
      >
        <div className="form-section">
          <h2>Ruta</h2>

          <div className="form-grid">
            <div className="form-field">
              <label htmlFor="origin">Origen</label>

              <input
                id="origin"
                type="text"
                value={origin}
                onChange={(event) => {
                  setOrigin(event.target.value)
                  clearError('origin')
                }}
                placeholder="Ej. Sevilla"
              />

              {errors.origin && (
                <span className="form-error">
                  {errors.origin}
                </span>
              )}
            </div>

            <div className="form-field">
              <label htmlFor="destination">Destino</label>

              <input
                id="destination"
                type="text"
                value={destination}
                onChange={(event) => {
                  setDestination(event.target.value)
                  clearError('destination')
                }}
                placeholder="Ej. Madrid"
              />

              {errors.destination && (
                <span className="form-error">
                  {errors.destination}
                </span>
              )}
            </div>
          </div>
        </div>

        <div className="form-section">
          <h2>Vehículo y horario</h2>

          <div className="form-field">
            <label htmlFor="vehicle">Vehículo</label>

            <input
              id="vehicle"
              type="text"
              value={vehicle}
              onChange={(event) => {
                setVehicle(event.target.value)
                clearError('vehicle')
              }}
              placeholder="Ej. Mercedes-Benz Sprinter"
            />

            {errors.vehicle && (
              <span className="form-error">
                {errors.vehicle}
              </span>
            )}
          </div>

          <div className="form-grid">
            <div className="form-field">
              <label htmlFor="departureDate">
                Fecha y hora de salida
              </label>

              <input
                id="departureDate"
                type="datetime-local"
                value={departureDate}
                onChange={(event) => {
                  setDepartureDate(event.target.value)
                  clearError('departureDate')
                }}
              />

              {errors.departureDate && (
                <span className="form-error">
                  {errors.departureDate}
                </span>
              )}
            </div>

            <div className="form-field">
              <label htmlFor="arrivalDate">
                Fecha y hora de llegada
              </label>

              <input
                id="arrivalDate"
                type="datetime-local"
                value={arrivalDate}
                onChange={(event) => {
                  setArrivalDate(event.target.value)
                  clearError('arrivalDate')
                }}
              />

              {errors.arrivalDate && (
                <span className="form-error">
                  {errors.arrivalDate}
                </span>
              )}
            </div>
          </div>
        </div>

        <div className="form-section">
          <h2>Capacidad disponible</h2>

          <div className="form-grid">
            <div className="form-field">
              <label htmlFor="availableWeightKg">
                Peso disponible (kg)
              </label>

              <input
                id="availableWeightKg"
                type="number"
                min="0"
                step="0.01"
                value={availableWeightKg}
                onChange={(event) => {
                  setAvailableWeightKg(event.target.value)
                  clearError('availableWeightKg')
                }}
                placeholder="1000"
              />

              {errors.availableWeightKg && (
                <span className="form-error">
                  {errors.availableWeightKg}
                </span>
              )}
            </div>

            <div className="form-field">
              <label htmlFor="availableVolumeM3">
                Volumen disponible (m³)
              </label>

              <input
                id="availableVolumeM3"
                type="number"
                min="0"
                step="0.1"
                value={availableVolumeM3}
                onChange={(event) => {
                  setAvailableVolumeM3(event.target.value)
                  clearError('availableVolumeM3')
                }}
                placeholder="5.5"
              />

              {errors.availableVolumeM3 && (
                <span className="form-error">
                  {errors.availableVolumeM3}
                </span>
              )}
            </div>
          </div>
        </div>

        <div className="form-actions">
          <button
            type="button"
            onClick={() => navigate('/trips')}
          >
            Cancelar
          </button>

          <button type="submit">
            Publicar viaje
          </button>
        </div>
      </form>
    </div>
  )
}

export default CreateTripPage