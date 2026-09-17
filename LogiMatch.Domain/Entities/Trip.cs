using LogiMatch.Domain.Enums;

namespace LogiMatch.Domain.Entities;

public class Trip
{
    public Guid Id { get; private set; }

    public Guid TransporterProfileId { get; private set; }

    public Guid VehicleId { get; private set; }

    public Guid OriginLocationId { get; private set; }

    public Guid DestinationLocationId { get; private set; }

    public DateTime DepartureDate { get; private set; }

    public DateTime EstimatedArrivalDate { get; private set; }

    public TripStatus Status { get; private set; }

    public decimal AvailableWeightKg { get; private set; }

    public decimal AvailableVolumeM3 { get; private set; }
    public decimal InitialAvailableWeightKg { get; private set; }
    public decimal InitialAvailableVolumeM3 { get; private set; }


    private Trip()
    {
    }

    public Trip(
        Guid transporterProfileId,
        Guid vehicleId,
        Guid originLocationId,
        Guid destinationLocationId,
        DateTime departureDate,
        DateTime estimatedArrivalDate,
        decimal availableWeightKg,
        decimal availableVolumeM3)
    {
        if (estimatedArrivalDate <= departureDate)
            throw new InvalidOperationException(
                "Estimated arrival must be after departure.");

        if (availableWeightKg <= 0)
            throw new ArgumentException(
                "Available weight must be greater than zero.",
                nameof(availableWeightKg));

        if (availableVolumeM3 <= 0)
            throw new ArgumentException(
                "Available volume must be greater than zero.",
                nameof(availableVolumeM3));

        Id = Guid.NewGuid();

        Status = TripStatus.Published;

        TransporterProfileId = transporterProfileId;
        VehicleId = vehicleId;

        OriginLocationId = originLocationId;
        DestinationLocationId = destinationLocationId;

        DepartureDate = departureDate;
        EstimatedArrivalDate = estimatedArrivalDate;

        InitialAvailableWeightKg = availableWeightKg;
        InitialAvailableVolumeM3 = availableVolumeM3;

        AvailableWeightKg = availableWeightKg;
        AvailableVolumeM3 = availableVolumeM3;
    }
    public void Start()
    {
        if (Status != TripStatus.Published)
            throw new InvalidOperationException(
                "Only published trips can be started.");

        Status = TripStatus.InProgress;
    }
    public void Complete()
    {
        if (Status != TripStatus.InProgress)
            throw new InvalidOperationException(
                "Only in-progress trips can be completed.");

        Status = TripStatus.Completed;
    }
    public void Cancel()
    {
        if (Status != TripStatus.Published)
            throw new InvalidOperationException(
                "Only published trips can be cancelled.");

        Status = TripStatus.Cancelled;
    }

    public void ReserveCapacity(
    decimal weightKg,
    decimal volumeM3)
    {
        if (Status != TripStatus.Published)
            throw new InvalidOperationException(
                "Only published trips can reserve capacity.");

        if (weightKg <= 0)
            throw new InvalidOperationException(
                "Weight must be greater than zero.");

        if (volumeM3 <= 0)
            throw new InvalidOperationException(
                "Volume must be greater than zero.");

        if (weightKg > AvailableWeightKg)
            throw new InvalidOperationException(
                "The trip does not have enough available weight.");

        if (volumeM3 > AvailableVolumeM3)
            throw new InvalidOperationException(
                "The trip does not have enough available volume.");

        AvailableWeightKg -= weightKg;
        AvailableVolumeM3 -= volumeM3;
    }

    public void ReleaseCapacity(decimal weightKg, decimal volumeM3)
    {
        if (weightKg <= 0)
            throw new ArgumentException(
                "Weight to release must be greater than zero.",
                nameof(weightKg));

        if (volumeM3 <= 0)
            throw new ArgumentException(
                "Volume to release must be greater than zero.",
                nameof(volumeM3));

        if (AvailableWeightKg + weightKg > InitialAvailableWeightKg)
            throw new InvalidOperationException(
                "Released weight exceeds the trip's initial available capacity.");

        if (AvailableVolumeM3 + volumeM3 > InitialAvailableVolumeM3)
            throw new InvalidOperationException(
                "Released volume exceeds the trip's initial available capacity.");

        AvailableWeightKg += weightKg;
        AvailableVolumeM3 += volumeM3;
    }
}