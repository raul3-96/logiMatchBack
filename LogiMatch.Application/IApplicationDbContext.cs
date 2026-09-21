using LogiMatch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace LogiMatch.Application;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Company> Companies { get; }
    DbSet<Location> Locations { get; }
    DbSet<TransportRequest> TransportRequests { get; }
    DbSet<Cargo> Cargos { get; }
    DbSet<TransporterProfile> TransporterProfiles { get; }
    DbSet<Vehicle> Vehicles { get; }
    DbSet<VehicleAvailability> VehicleAvailabilities { get; }
    DbSet<Trip> Trips { get; }
    DbSet<TripCargo> TripCargos { get; }
    DbSet<TransportOffer> TransportOffers { get; }
    DbSet<Booking> Bookings { get; }

    DatabaseFacade Database { get; }
    bool SupportsRowLocking => true; // Assuming PostgreSQL supports row locking

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);

}