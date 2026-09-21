using LogiMatch.Application.Common.Exceptions;
using LogiMatch.Application.Matching;
using LogiMatch.Application.Tests.Common;
using LogiMatch.Domain.Entities;
using LogiMatch.Domain.Enums;
using Xunit;

namespace LogiMatch.Application.Tests.Matching;

public class FindMatchingVehiclesHandlerTests
{
    [Fact]
    public async Task Handle_ShouldThrow_WhenTransportRequestDoesNotExist()
    {
        var db = TestDbContextFactory.Create();

        var handler = new FindMatchingVehiclesHandler(
            db,
            new MockCurrentUserService(Guid.NewGuid()));

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new FindMatchingVehiclesCommand
            {
                TransportRequestId = Guid.NewGuid()
            }));

        Assert.Equal(
            "The specified transport request does not exist.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenCurrentUserDoesNotOwnTransportRequest()
    {
        var db = TestDbContextFactory.Create();

        var request = await CreateValidRequest(db);

        var handler = new FindMatchingVehiclesHandler(
            db,
            new MockCurrentUserService(Guid.NewGuid()));

        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => handler.Handle(new FindMatchingVehiclesCommand
            {
                TransportRequestId = request.Id
            }));

        Assert.Equal(
            "You do not have permission to search vehicles for this transport request.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenRequestHasNoCargo()
    {
        var db = TestDbContextFactory.Create();

        var request = await CreateValidRequest(db);

        var handler = new FindMatchingVehiclesHandler(
            db,
            new MockCurrentUserService(request.CustomerId));

        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => handler.Handle(new FindMatchingVehiclesCommand
            {
                TransportRequestId = request.Id
            }));

        Assert.Equal(
            "The transport request has no cargo.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldReturnMatchingVehicle_WhenAllConditionsMatch()
    {
        var db = TestDbContextFactory.Create();

        var request = await CreateValidRequest(db);

        AddCargo(
            db,
            request,
            500m,
            2m);

        var vehicle = CreateVehicle(db);

        AddAvailability(
            db,
            vehicle,
            request.PickupDate.AddHours(-1),
            request.DeliveryDate!.Value.AddHours(1));

        await db.SaveChangesAsync();

        var handler = new FindMatchingVehiclesHandler(
            db,
            new MockCurrentUserService(request.CustomerId));

        var result = await handler.Handle(
            new FindMatchingVehiclesCommand
            {
                TransportRequestId = request.Id
            });

        var matches = GetMatches(result);

        Assert.Single(matches);

        Assert.Equal(
            vehicle.Id,
            GetProperty<Guid>(matches[0], "VehicleId"));
    }

    [Fact]
    public async Task Handle_ShouldReturnRequiredCargoTotals()
    {
        var db = TestDbContextFactory.Create();

        var request = await CreateValidRequest(db);

        AddCargo(db, request, 300m, 1.5m);
        AddCargo(db, request, 200m, 2.5m);

        var vehicle = CreateVehicle(db);

        AddAvailability(
            db,
            vehicle,
            request.PickupDate.AddHours(-1),
            request.DeliveryDate!.Value.AddHours(1));

        await db.SaveChangesAsync();

        var handler = new FindMatchingVehiclesHandler(
            db,
            new MockCurrentUserService(request.CustomerId));

        var result = await handler.Handle(
            new FindMatchingVehiclesCommand
            {
                TransportRequestId = request.Id
            });

        Assert.Equal(
            500m,
            GetProperty<decimal>(result, "RequiredWeightKg"));

        Assert.Equal(
            4m,
            GetProperty<decimal>(result, "RequiredVolumeM3"));
    }

    [Fact]
    public async Task Handle_ShouldReturnRequirementFlags()
    {
        var db = TestDbContextFactory.Create();

        var request = await CreateValidRequest(db);

        AddCargo(
            db,
            request,
            500m,
            2m,
            requiresRefrigeration: true,
            requiresTailLift: true);

        var vehicle = CreateVehicle(
            db,
            isRefrigerated: true,
            hasTailLift: true);

        AddAvailability(
            db,
            vehicle,
            request.PickupDate.AddHours(-1),
            request.DeliveryDate!.Value.AddHours(1));

        await db.SaveChangesAsync();

        var handler = new FindMatchingVehiclesHandler(
            db,
            new MockCurrentUserService(request.CustomerId));

        var result = await handler.Handle(
            new FindMatchingVehiclesCommand
            {
                TransportRequestId = request.Id
            });

        Assert.True(
            GetProperty<bool>(
                result,
                "RequiresRefrigeration"));

        Assert.True(
            GetProperty<bool>(
                result,
                "RequiresTailLift"));
    }

    [Fact]
    public async Task Handle_ShouldNotMatch_WhenWeightIsInsufficient()
    {
        var db = TestDbContextFactory.Create();

        var request = await CreateValidRequest(db);

        AddCargo(db, request, 1001m, 2m);

        var vehicle = CreateVehicle(
            db,
            maxWeightKg: 1000m);

        AddAvailability(
            db,
            vehicle,
            request.PickupDate.AddHours(-1),
            request.DeliveryDate!.Value.AddHours(1));

        await db.SaveChangesAsync();

        var handler = new FindMatchingVehiclesHandler(
            db,
            new MockCurrentUserService(request.CustomerId));

        var result = await handler.Handle(
            new FindMatchingVehiclesCommand
            {
                TransportRequestId = request.Id
            });

        Assert.Empty(GetMatches(result));
    }

    [Fact]
    public async Task Handle_ShouldNotMatch_WhenVolumeIsInsufficient()
    {
        var db = TestDbContextFactory.Create();

        var request = await CreateValidRequest(db);

        AddCargo(db, request, 500m, 5.1m);

        var vehicle = CreateVehicle(
            db,
            maxVolumeM3: 5m);

        AddAvailability(
            db,
            vehicle,
            request.PickupDate.AddHours(-1),
            request.DeliveryDate!.Value.AddHours(1));

        await db.SaveChangesAsync();

        var handler = new FindMatchingVehiclesHandler(
            db,
            new MockCurrentUserService(request.CustomerId));

        var result = await handler.Handle(
            new FindMatchingVehiclesCommand
            {
                TransportRequestId = request.Id
            });

        Assert.Empty(GetMatches(result));
    }

    [Fact]
    public async Task Handle_ShouldNotMatch_WhenRefrigerationIsRequired()
    {
        var db = TestDbContextFactory.Create();

        var request = await CreateValidRequest(db);

        AddCargo(
            db,
            request,
            500m,
            2m,
            requiresRefrigeration: true);

        var vehicle = CreateVehicle(
            db,
            isRefrigerated: false);

        AddAvailability(
            db,
            vehicle,
            request.PickupDate.AddHours(-1),
            request.DeliveryDate!.Value.AddHours(1));

        await db.SaveChangesAsync();

        var handler = new FindMatchingVehiclesHandler(
            db,
            new MockCurrentUserService(request.CustomerId));

        var result = await handler.Handle(
            new FindMatchingVehiclesCommand
            {
                TransportRequestId = request.Id
            });

        Assert.Empty(GetMatches(result));
    }

    [Fact]
    public async Task Handle_ShouldNotMatch_WhenTailLiftIsRequired()
    {
        var db = TestDbContextFactory.Create();

        var request = await CreateValidRequest(db);

        AddCargo(
            db,
            request,
            500m,
            2m,
            requiresTailLift: true);

        var vehicle = CreateVehicle(
            db,
            hasTailLift: false);

        AddAvailability(
            db,
            vehicle,
            request.PickupDate.AddHours(-1),
            request.DeliveryDate!.Value.AddHours(1));

        await db.SaveChangesAsync();

        var handler = new FindMatchingVehiclesHandler(
            db,
            new MockCurrentUserService(request.CustomerId));

        var result = await handler.Handle(
            new FindMatchingVehiclesCommand
            {
                TransportRequestId = request.Id
            });

        Assert.Empty(GetMatches(result));
    }

    [Fact]
    public async Task Handle_ShouldMatch_WhenVehicleHasRequiredRefrigerationAndTailLift()
    {
        var db = TestDbContextFactory.Create();

        var request = await CreateValidRequest(db);

        AddCargo(
            db,
            request,
            500m,
            2m,
            requiresRefrigeration: true,
            requiresTailLift: true);

        var vehicle = CreateVehicle(
            db,
            isRefrigerated: true,
            hasTailLift: true);

        AddAvailability(
            db,
            vehicle,
            request.PickupDate,
            request.DeliveryDate!.Value);

        await db.SaveChangesAsync();

        var handler = new FindMatchingVehiclesHandler(
            db,
            new MockCurrentUserService(request.CustomerId));

        var result = await handler.Handle(
            new FindMatchingVehiclesCommand
            {
                TransportRequestId = request.Id
            });

        Assert.Single(GetMatches(result));
    }

    [Fact]
    public async Task Handle_ShouldNotMatch_WhenNoVehicleAvailabilityExists()
    {
        var db = TestDbContextFactory.Create();

        var request = await CreateValidRequest(db);

        AddCargo(db, request, 500m, 2m);

        CreateVehicle(db);

        await db.SaveChangesAsync();

        var handler = new FindMatchingVehiclesHandler(
            db,
            new MockCurrentUserService(request.CustomerId));

        var result = await handler.Handle(
            new FindMatchingVehiclesCommand
            {
                TransportRequestId = request.Id
            });

        Assert.Empty(GetMatches(result));
    }

    [Fact]
    public async Task Handle_ShouldNotMatch_WhenAvailabilityStartsAfterPickup()
    {
        var db = TestDbContextFactory.Create();

        var request = await CreateValidRequest(db);

        AddCargo(db, request, 500m, 2m);

        var vehicle = CreateVehicle(db);

        AddAvailability(
            db,
            vehicle,
            request.PickupDate.AddMinutes(1),
            request.DeliveryDate!.Value.AddHours(1));

        await db.SaveChangesAsync();

        var handler = new FindMatchingVehiclesHandler(
            db,
            new MockCurrentUserService(request.CustomerId));

        var result = await handler.Handle(
            new FindMatchingVehiclesCommand
            {
                TransportRequestId = request.Id
            });

        Assert.Empty(GetMatches(result));
    }

    [Fact]
    public async Task Handle_ShouldNotMatch_WhenAvailabilityEndsBeforeDelivery()
    {
        var db = TestDbContextFactory.Create();

        var request = await CreateValidRequest(db);

        AddCargo(db, request, 500m, 2m);

        var vehicle = CreateVehicle(db);

        AddAvailability(
            db,
            vehicle,
            request.PickupDate.AddHours(-1),
            request.DeliveryDate!.Value.AddMinutes(-1));

        await db.SaveChangesAsync();

        var handler = new FindMatchingVehiclesHandler(
            db,
            new MockCurrentUserService(request.CustomerId));

        var result = await handler.Handle(
            new FindMatchingVehiclesCommand
            {
                TransportRequestId = request.Id
            });

        Assert.Empty(GetMatches(result));
    }

    [Fact]
    public async Task Handle_ShouldMatch_WhenAvailabilityStartsExactlyAtPickupAndEndsExactlyAtDelivery()
    {
        var db = TestDbContextFactory.Create();

        var request = await CreateValidRequest(db);

        AddCargo(db, request, 500m, 2m);

        var vehicle = CreateVehicle(db);

        AddAvailability(
            db,
            vehicle,
            request.PickupDate,
            request.DeliveryDate!.Value);

        await db.SaveChangesAsync();

        var handler = new FindMatchingVehiclesHandler(
            db,
            new MockCurrentUserService(request.CustomerId));

        var result = await handler.Handle(
            new FindMatchingVehiclesCommand
            {
                TransportRequestId = request.Id
            });

        Assert.Single(GetMatches(result));
    }

    [Fact]
    public async Task Handle_ShouldReturnOnlyMatchingVehicles()
    {
        var db = TestDbContextFactory.Create();

        var request = await CreateValidRequest(db);

        AddCargo(db, request, 500m, 2m);

        var matchingVehicle = CreateVehicle(
            db,
            maxWeightKg: 1000m);

        var nonMatchingVehicle = CreateVehicle(
            db,
            maxWeightKg: 400m);

        AddAvailability(
            db,
            matchingVehicle,
            request.PickupDate.AddHours(-1),
            request.DeliveryDate!.Value.AddHours(1));

        AddAvailability(
            db,
            nonMatchingVehicle,
            request.PickupDate.AddHours(-1),
            request.DeliveryDate!.Value.AddHours(1));

        await db.SaveChangesAsync();

        var handler = new FindMatchingVehiclesHandler(
            db,
            new MockCurrentUserService(request.CustomerId));

        var result = await handler.Handle(
            new FindMatchingVehiclesCommand
            {
                TransportRequestId = request.Id
            });

        var matches = GetMatches(result);

        Assert.Single(matches);

        Assert.Equal(
            matchingVehicle.Id,
            GetProperty<Guid>(
                matches[0],
                "VehicleId"));
    }

    [Fact]
    public async Task Handle_ShouldReturnVehicleDetails_WithoutLicensePlate()
    {
        var db = TestDbContextFactory.Create();

        var request = await CreateValidRequest(db);

        AddCargo(db, request, 500m, 2m);

        var vehicle = CreateVehicle(
            db,
            type: VehicleType.LargeVan,
            brand: "Mercedes",
            model: "Sprinter",
            licensePlate: "TEST123",
            maxWeightKg: 1500m,
            maxVolumeM3: 10m,
            hasTailLift: true,
            isRefrigerated: true);

        AddAvailability(
            db,
            vehicle,
            request.PickupDate.AddHours(-1),
            request.DeliveryDate!.Value.AddHours(1));

        await db.SaveChangesAsync();

        var handler = new FindMatchingVehiclesHandler(
            db,
            new MockCurrentUserService(request.CustomerId));

        var result = await handler.Handle(
            new FindMatchingVehiclesCommand
            {
                TransportRequestId = request.Id
            });

        var matches = GetMatches(result);

        Assert.Single(matches);

        var vehicleResult =
            GetProperty<object>(
                matches[0],
                "Vehicle");

        Assert.Equal(
            VehicleType.LargeVan,
            GetProperty<VehicleType>(
                vehicleResult,
                "Type"));

        Assert.Equal(
            "Mercedes",
            GetProperty<string>(
                vehicleResult,
                "Brand"));

        Assert.Equal(
            "Sprinter",
            GetProperty<string>(
                vehicleResult,
                "Model"));

        Assert.Equal(
            1500m,
            GetProperty<decimal>(
                vehicleResult,
                "MaxWeightKg"));

        Assert.Equal(
            10m,
            GetProperty<decimal>(
                vehicleResult,
                "MaxVolumeM3"));

        Assert.True(
            GetProperty<bool>(
                vehicleResult,
                "HasTailLift"));

        Assert.True(
            GetProperty<bool>(
                vehicleResult,
                "IsRefrigerated"));
    }

    [Fact]
    public async Task Handle_ShouldReturnTransporterInformation_WithoutPrivateData()
    {
        var db = TestDbContextFactory.Create();

        var user = new User(
            "john@example.com",
            "John",
            "Doe",
            "600123456");

        db.Users.Add(user);

        var company = new Company(
            "Transport Company",
            "ESB12345678",
            "company@example.com",
            "600987654");

        db.Companies.Add(company);

        var profile = new TransporterProfile(
            user.Id,
            company.Id);

        db.TransporterProfiles.Add(profile);

        var request = new TransportRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddHours(2),
            DateTime.UtcNow.AddHours(10));

        db.TransportRequests.Add(request);

        AddCargo(
            db,
            request,
            500m,
            2m);

        var vehicle = CreateVehicle(
            db,
            transporterProfileId: profile.Id);

        AddAvailability(
            db,
            vehicle,
            request.PickupDate.AddHours(-1),
            request.DeliveryDate!.Value.AddHours(1));

        await db.SaveChangesAsync();

        var handler = new FindMatchingVehiclesHandler(
            db,
            new MockCurrentUserService(request.CustomerId));

        var result = await handler.Handle(
            new FindMatchingVehiclesCommand
            {
                TransportRequestId = request.Id
            });

        var matches = GetMatches(result);

        Assert.Single(matches);

        var transporter =
            GetProperty<object>(
                matches[0],
                "Transporter");

        Assert.NotNull(transporter);

        var transporterUser =
            GetProperty<object>(
                transporter,
                "User");

        Assert.Equal(
            "John",
            GetProperty<string>(
                transporterUser,
                "FirstName"));

        Assert.Equal(
            "Doe",
            GetProperty<string>(
                transporterUser,
                "LastName"));

        var transporterCompany =
            GetProperty<object>(
                transporter,
                "Company");

        Assert.NotNull(transporterCompany);

        Assert.Equal(
            "Transport Company",
            GetProperty<string>(
                transporterCompany,
                "Name"));
    }

    [Fact]
    public async Task Handle_ShouldReturnNullCompany_WhenTransporterHasNoCompany()
    {
        var db = TestDbContextFactory.Create();

        var user = new User(
            "john@example.com",
            "John",
            "Doe",
            "600123456");

        db.Users.Add(user);

        var profile = new TransporterProfile(user.Id);

        db.TransporterProfiles.Add(profile);

        var request = await CreateValidRequest(db);

        AddCargo(db, request, 500m, 2m);

        var vehicle = CreateVehicle(
            db,
            transporterProfileId: profile.Id);

        AddAvailability(
            db,
            vehicle,
            request.PickupDate.AddHours(-1),
            request.DeliveryDate!.Value.AddHours(1));

        await db.SaveChangesAsync();

        var handler = new FindMatchingVehiclesHandler(
            db,
            new MockCurrentUserService(request.CustomerId));

        var result = await handler.Handle(
            new FindMatchingVehiclesCommand
            {
                TransportRequestId = request.Id
            });

        var matches = GetMatches(result);

        Assert.Single(matches);

        var transporter =
            GetProperty<object>(
                matches[0],
                "Transporter");

        Assert.NotNull(transporter);

        var company =
            GetProperty<object>(
                transporter,
                "Company");

        Assert.Null(company);
    }

    [Fact]
    public async Task Handle_ShouldNotMatch_WhenRequestHasNoDeliveryDeadline()
    {
        var db = TestDbContextFactory.Create();

        var request = new TransportRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddHours(2),
            null);

        db.TransportRequests.Add(request);

        AddCargo(db, request, 500m, 2m);

        var vehicle = CreateVehicle(db);

        AddAvailability(
            db,
            vehicle,
            request.PickupDate.AddHours(-1),
            request.PickupDate.AddDays(10));

        await db.SaveChangesAsync();

        var handler = new FindMatchingVehiclesHandler(
            db,
            new MockCurrentUserService(request.CustomerId));

        var result = await handler.Handle(
            new FindMatchingVehiclesCommand
            {
                TransportRequestId = request.Id
            });

        Assert.Empty(GetMatches(result));
    }

    [Fact]
    public async Task Handle_ShouldReturnEmpty_WhenNoVehiclesMatch()
    {
        var db = TestDbContextFactory.Create();

        var request = await CreateValidRequest(db);

        AddCargo(db, request, 500m, 2m);

        var vehicle = CreateVehicle(
            db,
            maxWeightKg: 400m);

        AddAvailability(
            db,
            vehicle,
            request.PickupDate.AddHours(-1),
            request.DeliveryDate!.Value.AddHours(1));

        await db.SaveChangesAsync();

        var handler = new FindMatchingVehiclesHandler(
            db,
            new MockCurrentUserService(request.CustomerId));

        var result = await handler.Handle(
            new FindMatchingVehiclesCommand
            {
                TransportRequestId = request.Id
            });

        Assert.Empty(GetMatches(result));
    }

    private static async Task<TransportRequest> CreateValidRequest(
        TestDbContext db)
    {
        var request = new TransportRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddHours(2),
            DateTime.UtcNow.AddHours(10));

        db.TransportRequests.Add(request);

        await db.SaveChangesAsync();

        return request;
    }

    private static void AddCargo(
        TestDbContext db,
        TransportRequest request,
        decimal weightKg,
        decimal volumeM3,
        bool requiresRefrigeration = false,
        bool requiresTailLift = false)
    {
        var cargo = new Cargo(
            request.Id,
            "Test cargo",
            weightKg,
            volumeM3,
            1,
            requiresRefrigeration,
            requiresTailLift);

        db.Cargos.Add(cargo);
    }

    private static Vehicle CreateVehicle(
        TestDbContext db,
        Guid? transporterProfileId = null,
        VehicleType type = VehicleType.Van,
        string brand = "Mercedes",
        string model = "Sprinter",
        string licensePlate = "TEST123",
        decimal maxWeightKg = 1500m,
        decimal maxVolumeM3 = 10m,
        bool hasTailLift = false,
        bool isRefrigerated = false)
    {
        var vehicle = new Vehicle(
            transporterProfileId ?? Guid.NewGuid(),
            type,
            brand,
            model,
            licensePlate,
            maxWeightKg,
            maxVolumeM3,
            6m,
            2m,
            2.5m,
            hasTailLift,
            isRefrigerated);

        db.Vehicles.Add(vehicle);

        return vehicle;
    }

    private static void AddAvailability(
        TestDbContext db,
        Vehicle vehicle,
        DateTime availableFrom,
        DateTime availableTo)
    {
        var availability = new VehicleAvailability(
            vehicle.Id,
            availableFrom,
            availableTo);

        db.VehicleAvailabilities.Add(availability);
    }

    private static object[] GetMatches(object result)
    {
        var property =
            result.GetType().GetProperty("Matches")!;

        var value =
            property.GetValue(result)!;

        return ((System.Collections.IEnumerable)value)
            .Cast<object>()
            .ToArray();
    }

    private static T GetProperty<T>(
        object obj,
        string propertyName)
    {
        var property =
            obj.GetType().GetProperty(propertyName)!;

        return (T)property.GetValue(obj)!;
    }
}