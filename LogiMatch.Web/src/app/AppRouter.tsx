import { BrowserRouter, Routes, Route } from 'react-router-dom'

import MainLayout from '../layouts/MainLayout'
import HomePage from '../pages/HomePage'
import TransportRequestsPage from '../features/transportRequests/pages/TransportRequestsPage'
import CreateTransportRequestPage from '../features/transportRequests/pages/CreateTransportRequestPage'
import MatchingTripsPage from '../features/transportRequests/pages/MatchingTripsPage'
import TripsPage from '../features/trips/pages/TripsPage'
import CreateTripPage from '../features/trips/pages/CreateTripPage'
import TripDetailPage from '../features/trips/pages/TripDetailPage'
import BookingsPage from '../features/bookings/pages/BookingsPage'
import BookingDetailPage from '../features/bookings/pages/BookingDetailPage'

function PlaceholderPage({ title }: { title: string }) {
  return (
    <div>
      <h1>{title}</h1>
      <p>Esta sección estará disponible próximamente.</p>
    </div>
  )
}

function AppRouter() {
  return (
    <BrowserRouter>
      <Routes>
        <Route element={<MainLayout />}>
          <Route path="/" element={<HomePage />} />

          <Route
            path="/transport-requests"
            element={<TransportRequestsPage />}
          />

          <Route
            path="/transport-requests/create"
            element={<CreateTransportRequestPage />}
          />

          <Route
            path="/transport-requests/:id/matching-trips"
            element={<MatchingTripsPage />}
          />

          <Route
            path="/trips"
            element={<TripsPage />}
          />

          <Route
            path="/trips/create"
            element={<CreateTripPage />}
          />

          <Route
            path="/trips/:id"
            element={<TripDetailPage />}
          />

          <Route
            path="/vehicles"
            element={<PlaceholderPage title="Vehículos" />}
          />

          <Route
            path="/bookings"
            element={<BookingsPage />}
          />

          <Route 
            path="/bookings/:id" 
            element={<BookingDetailPage />} 
          />

          <Route
            path="/profile"
            element={<PlaceholderPage title="Perfil" />}
          />
        </Route>
      </Routes>
    </BrowserRouter>
  )
}

export default AppRouter