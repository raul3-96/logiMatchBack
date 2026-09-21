namespace LogiMatch.Application.Trips;

public class CreateTripCommand
{
    public Guid TransporterProfileId { get; set; }

    public Guid VehicleId { get; set; }

    public Guid OriginLocationId { get; set; }

    public Guid DestinationLocationId { get; set; }

    public DateTime DepartureDate { get; set; }

    public DateTime EstimatedArrivalDate { get; set; }

    public decimal AvailableWeightKg { get; set; }

    public decimal AvailableVolumeM3 { get; set; }
}