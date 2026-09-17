import { NavLink, Outlet } from 'react-router-dom'

function MainLayout() {
  return (
    <div className="main-layout">
      <header className="main-header">
        <div className="logo">
          LogiMatch
        </div>

        <div className="header-user">
          Usuario
        </div>
      </header>

      <div className="main-content">
        <aside className="sidebar">
          <nav>
            <NavLink to="/">
              Dashboard
            </NavLink>

            <NavLink to="/transport-requests">
              Mis cargas
            </NavLink>

            <NavLink to="/trips">
              Mis viajes
            </NavLink>

            <NavLink to="/vehicles">
              Vehículos
            </NavLink>

            <NavLink to="/bookings">
              Reservas
            </NavLink>

            <NavLink to="/profile">
              Perfil
            </NavLink>
          </nav>
        </aside>

        <main className="page-content">
          <Outlet />
        </main>
      </div>
    </div>
  )
}

export default MainLayout