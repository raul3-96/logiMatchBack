namespace LogiMatch.Application.VehicleAvailabilities;

public class CreateVehicleAvailabilityCommand
{
    public Guid VehicleId { get; set; }

    public DateTime AvailableFrom { get; set; }

    public DateTime AvailableTo { get; set; }
}