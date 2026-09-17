namespace LogiMatch.Domain.Entities;

public class VehicleAvailability
{
    public Guid Id { get; private set; }

    public Guid VehicleId { get; private set; }

    public DateTime AvailableFrom { get; private set; }

    public DateTime AvailableTo { get; private set; }

    private VehicleAvailability()
    {
    }

    public VehicleAvailability(
        Guid vehicleId,
        DateTime availableFrom,
        DateTime availableTo)
    {
        if (availableTo <= availableFrom)
            throw new InvalidOperationException(
                "AvailableTo must be after AvailableFrom.");

        Id = Guid.NewGuid();
        VehicleId = vehicleId;
        AvailableFrom = availableFrom;
        AvailableTo = availableTo;
    }
}