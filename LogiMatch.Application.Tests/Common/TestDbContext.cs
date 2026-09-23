using LogiMatch.Application;
using LogiMatch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LogiMatch.Application.Tests.Common;

public class TestDbContext : DbContext, IApplicationDbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options)
    : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<TransportRequest> TransportRequests => Set<TransportRequest>();
    public DbSet<Cargo> Cargos => Set<Cargo>();
    public DbSet<TransporterProfile> TransporterProfiles => Set<TransporterProfile>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<VehicleAvailability> VehicleAvailabilities => Set<VehicleAvailability>();
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<TripCargo> TripCargos => Set<TripCargo>();
    public DbSet<TransportOffer> TransportOffers => Set<TransportOffer>();
    public DbSet<Booking> Bookings => Set<Booking>();

    public DatabaseFacade Database => base.Database;
    public bool SupportsRowLocking => false;

    public DbSet<CompanyMember> CompanyMembers => Set<CompanyMember>();
}