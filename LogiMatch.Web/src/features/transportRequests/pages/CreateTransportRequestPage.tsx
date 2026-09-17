import { useState } from 'react'
import type { TransportRequest } from '../types/transportRequest.types'

function CreateTransportRequestPage() {
  const [origin, setOrigin] = useState('')
  const [destination, setDestination] = useState('')
  const [cargoDescription, setCargoDescription] = useState('')
  const [weightKg, setWeightKg] = useState('')
  const [volumeM3, setVolumeM3] = useState('')
  const [tailLift, setTailLift] = useState(false)
  const [pickupDate, setPickupDate] = useState('')
  const [deliveryDate, setDeliveryDate] = useState('')
  const [errors, setErrors] = useState<Record<string, string>>({})

    const handleSubmit = (event: React.FormEvent<HTMLFormElement>) => {
        event.preventDefault()

        const newErrors: Record<string, string> = {}

        if (!origin.trim()) {
            newErrors.origin = 'El origen es obligatorio'
        }

        if (!destination.trim()) {
            newErrors.destination = 'El destino es obligatorio'
        }

        if (!cargoDescription.trim()) {
            newErrors.cargoDescription = 'La descripción es obligatoria'
        }

        if (!weightKg) {
            newErrors.weightKg = 'El peso es obligatorio'
        } else if (Number(weightKg) <= 0) {
            newErrors.weightKg = 'El peso debe ser mayor que 0'
        }

        if (!volumeM3) {
            newErrors.volumeM3 = 'El volumen es obligatorio'
        } else if (Number(volumeM3) <= 0) {
            newErrors.volumeM3 = 'El volumen debe ser mayor que 0'
        }

        if (!pickupDate) {
            newErrors.pickupDate = 'La fecha de recogida es obligatoria'
        }

        if (!deliveryDate) {
            newErrors.deliveryDate = 'La fecha de entrega es obligatoria'
        } else if (pickupDate && deliveryDate < pickupDate) {
            newErrors.deliveryDate =
            'La fecha de entrega no puede ser anterior a la recogida'
        }

        setErrors(newErrors)

        if (Object.keys(newErrors).length > 0) {
            return
        }

        const newRequest: TransportRequest = {
            id: crypto.randomUUID(),
            origin: origin.trim(),
            destination: destination.trim(),
            cargoDescription: cargoDescription.trim(),
            weightKg: Number(weightKg),
            volumeM3: Number(volumeM3),
            status: 'Pendiente',
            pickupDate,
            deliveryDate,
        }

        const storedRequests = localStorage.getItem('transportRequests')

        const requests: TransportRequest[] = storedRequests
            ? JSON.parse(storedRequests)
            : []

        requests.push(newRequest)

        localStorage.setItem(
            'transportRequests',
            JSON.stringify(requests),
        )

        window.location.href = '/transport-requests'
    }

    const clearError = (field: string) => {
        setErrors((currentErrors) => {
            const newErrors = { ...currentErrors }
            delete newErrors[field]
            return newErrors
        })
    }

  return (
    <div className="create-transport-request-page">
      <div className="page-header">
        <div>
          <h1>Nueva carga</h1>
          <p>Crea una nueva solicitud de transporte</p>
        </div>
      </div>

      <form
        className="transport-request-form"
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
                        clearError('origin')}}
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
                onChange={(event) =>{
                    setDestination(event.target.value)
                    clearError('destination')}
                }
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
          <h2>Carga</h2>

          <div className="form-field">
            <label htmlFor="cargoDescription">
              Descripción
            </label>

            <input
              id="cargoDescription"
              type="text"
              value={cargoDescription}
              onChange={(event) =>{
                setCargoDescription(event.target.value)
                clearError('cargoDescription')}
              }
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
                <label htmlFor="weightKg">Peso (kg)</label>

                <input
                    id="weightKg"
                    type="number"
                    min="0"
                    value={weightKg}
                    onChange={(event) =>{
                        setWeightKg(event.target.value)
                        clearError('weightKg')}
                    }
                    placeholder="850"
                />
                {errors.weightKg && (
                        <span className="form-error">
                            {errors.weightKg}
                        </span>
                )}
                </div>

                <div className="form-field">
                <label htmlFor="volumeM3">Volumen (m³)</label>

                <input
                    id="volumeM3"
                    type="number"
                    min="0"
                    step="0.1"
                    value={volumeM3}
                    onChange={(event) =>{
                        setVolumeM3(event.target.value)
                        clearError('volumeM3')}
                    }
                    placeholder="4.5"
                />
                {errors.volumeM3 && (
                    <span className="form-error">
                        {errors.volumeM3}
                    </span>
                )}
                </div>
            </div>

            <div className="form-grid">
                <div className="form-field">
                    <label htmlFor="pickupDate">
                    Fecha de recogida
                    </label>

                    <input
                    id="pickupDate"
                    type="date"
                    value={pickupDate}
                    onChange={(event) => {
                        setPickupDate(event.target.value)
                        clearError('pickupDate')
                    }}
                    />

                    {errors.pickupDate && (
                    <span className="form-error">
                        {errors.pickupDate}
                    </span>
                    )}
                </div>

                <div className="form-field">
                    <label htmlFor="deliveryDate">
                    Fecha de entrega
                    </label>

                    <input
                    id="deliveryDate"
                    type="date"
                    value={deliveryDate}
                    onChange={(event) => {
                        setDeliveryDate(event.target.value)
                        clearError('deliveryDate')
                    }}
                    />

                    {errors.deliveryDate && (
                    <span className="form-error">
                        {errors.deliveryDate}
                    </span>
                    )}
                </div>
            </div>

          <label className="checkbox-field">
            <input
              type="checkbox"
              checked={tailLift}
              onChange={(event) =>
                setTailLift(event.target.checked)
              }
            />

            <span>Necesita plataforma elevadora</span>
          </label>
        </div>

        <div className="form-actions">
          <button
            type="button"
            onClick={() => window.history.back()}
          >
            Cancelar
          </button>

          <button type="submit">
            Crear carga
          </button>
        </div>
      </form>
    </div>
  )
}

export default CreateTransportRequestPage