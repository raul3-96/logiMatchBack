function HomePage() {
  return (
    <div className="dashboard">
      <div className="dashboard-header">
        <div>
          <h1>Dashboard</h1>
          <p>Bienvenido a LogiMatch</p>
        </div>
      </div>

      <div className="dashboard-stats">
        <div className="stat-card">
          <span>Mis cargas</span>
          <strong>3</strong>
        </div>

        <div className="stat-card">
          <span>Mis viajes</span>
          <strong>2</strong>
        </div>

        <div className="stat-card">
          <span>Vehículos</span>
          <strong>4</strong>
        </div>

        <div className="stat-card">
          <span>Reservas</span>
          <strong>5</strong>
        </div>
      </div>

      <section className="dashboard-section">
        <h2>Actividad reciente</h2>

        <div className="activity-list">
          <div className="activity-item">
            <div>
              <strong>Carga Sevilla → Madrid</strong>
              <span>Solicitud de transporte</span>
            </div>

            <span>Pendiente</span>
          </div>

          <div className="activity-item">
            <div>
              <strong>Viaje Madrid → Valencia</strong>
              <span>Viaje publicado</span>
            </div>

            <span>Publicado</span>
          </div>

          <div className="activity-item">
            <div>
              <strong>Reserva #00025</strong>
              <span>Reserva de capacidad</span>
            </div>

            <span>Confirmada</span>
          </div>
        </div>
      </section>
    </div>
  )
}

export default HomePage