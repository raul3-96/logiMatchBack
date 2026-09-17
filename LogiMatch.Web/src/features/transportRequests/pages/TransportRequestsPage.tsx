import { useNavigate } from 'react-router-dom'
import type { TransportRequest } from '../types/transportRequest.types'

const mockTransportRequests: TransportRequest[] = [
  {
    id: '1',
    origin: 'Sevilla',
    destination: 'Madrid',
    cargoDescription: 'Cajas de material',
    weightKg: 850,
    volumeM3: 4.5,
    status: 'Pendiente',pickupDate: '2026-09-20',
    deliveryDate: '2026-09-20',
  },
  {
    id: '2',
    origin: 'Sevilla',
    destination: 'Córdoba',
    cargoDescription: 'Mobiliario',
    weightKg: 300,
    volumeM3: 2,
    status: 'Ofertada',pickupDate: '2026-09-20',
    deliveryDate: '2026-09-20',
  },
  {
    id: '3',
    origin: 'Madrid',
    destination: 'Valencia',
    cargoDescription: 'Equipamiento industrial',
    weightKg: 1200,
    volumeM3: 7,
    status: 'Reservada',pickupDate: '2026-09-20',
    deliveryDate: '2026-09-20',
  },
]

function TransportRequestsPage() {
  const navigate = useNavigate()

  const transportRequests: TransportRequest[] = (() => {
    const storedRequests = localStorage.getItem('transportRequests')

    if (storedRequests) {
      return JSON.parse(storedRequests)
    }

    return mockTransportRequests
  })()

  return (
    <div className="transport-requests-page">
      <div className="page-header">
        <div>
          <h1>Mis cargas</h1>
          <p>Gestiona tus solicitudes de transporte</p>
        </div>

        <button
          type="button"
          onClick={() => navigate('/transport-requests/create')}
        >
          + Nueva carga
        </button>
      </div>

      <div className="transport-request-list">
        {transportRequests.map((request) => (
          <div
            className="transport-request-card"
            key={request.id}
          >
            <div className="route">
              <strong>{request.origin}</strong>
              <span>→</span>
              <strong>{request.destination}</strong>
            </div>

            <div className="cargo-description">
              {request.cargoDescription}
            </div>

            <div className="cargo-data">
              <span>{request.weightKg} kg</span>
              <span>{request.volumeM3} m³</span>
            </div>
            <div className="cargo-dates">
              <span>
                Recogida: {request.pickupDate}
              </span>

              <span>
                Entrega: {request.deliveryDate}
              </span>
            </div>

            <button
              type="button"
              onClick={() =>
                navigate(
                  `/transport-requests/${request.id}/matching-trips`,
                )
              }
            >
              Buscar viajes
            </button>
            <div className="request-status">
              {request.status}
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}

export default TransportRequestsPage