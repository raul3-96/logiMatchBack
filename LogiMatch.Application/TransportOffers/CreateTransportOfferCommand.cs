namespace LogiMatch.Application.TransportOffers;

public class CreateTransportOfferCommand
{
    public Guid TransportRequestId { get; set; }

    public Guid TransporterProfileId { get; set; }

    public Guid VehicleId { get; set; }

    public decimal Price { get; set; }

    public DateTime EstimatedPickupDate { get; set; }

    public DateTime EstimatedDeliveryDate { get; set; }
}