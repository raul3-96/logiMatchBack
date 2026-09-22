using LogiMatch.Application;
using LogiMatch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace LogiMatch.Infrastructure.Persistence.Configurations;

public class LogiMatchDbContext : DbContext, IApplicationDbContext
{
    public LogiMatchDbContext(DbContextOptions<LogiMatchDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<CompanyMember> CompanyMembers => Set<CompanyMember>();
    public DbSet<TransporterProfile> TransporterProfiles => Set<TransporterProfile>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<VehicleAvailability> VehicleAvailabilities => Set<VehicleAvailability>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<TransportRequest> TransportRequests => Set<TransportRequest>();
    public DbSet<Cargo> Cargos => Set<Cargo>();
    public DbSet<TransportOffer> TransportOffers => Set<TransportOffer>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<TripCargo> TripCargos => Set<TripCargo>();
    public DatabaseFacade Database => base.Database;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LogiMatchDbContext).Assembly);
    }
}