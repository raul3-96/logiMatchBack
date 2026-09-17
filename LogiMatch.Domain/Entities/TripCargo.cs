using LogiMatch.Domain;
using LogiMatch.Domain.Enums;

namespace LogiMatch.Domain.Entities;

public class TripCargo
{
    public Guid Id { get; private set; }

    public Guid TripId { get; private set; }

    public Guid TransportRequestId { get; private set; }

    public decimal WeightKg { get; private set; }

    public decimal VolumeM3 { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public TripCargoStatus Status { get; private set; }

    private TripCargo()
    {
    }

    public TripCargo(
        Guid tripId,
        Guid transportRequestId,
        decimal weightKg,
        decimal volumeM3)
    {
        if (weightKg <= 0)
            throw new InvalidOperationException(
                "Weight must be greater than zero.");

        if (volumeM3 <= 0)
            throw new InvalidOperationException(
                "Volume must be greater than zero.");

        Id = Guid.NewGuid();

        TripId = tripId;
        TransportRequestId = transportRequestId;

        WeightKg = weightKg;
        VolumeM3 = volumeM3;

        CreatedAt = DateTime.UtcNow;

        Status = TripCargoStatus.Reserved;
    }

    public void Start()
    {
        if (Status != TripCargoStatus.Reserved)
            throw new InvalidOperationException(
                "Only reserved trip cargo can be started.");

        Status = TripCargoStatus.InProgress;
    }

    public void Complete()
    {
        if (Status != TripCargoStatus.InProgress)
            throw new InvalidOperationException(
                "Only in-progress trip cargo can be completed.");

        Status = TripCargoStatus.Completed;
    }

    public void Cancel()
    {
        if (Status != TripCargoStatus.Reserved)
            throw new InvalidOperationException(
                "Only reserved trip cargo can be cancelled.");

        Status = TripCargoStatus.Cancelled;
    }
}