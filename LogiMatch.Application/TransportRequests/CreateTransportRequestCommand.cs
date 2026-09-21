namespace LogiMatch.Application.TransportRequests;

public class CreateTransportRequestCommand
{
    public Guid CustomerId { get; set; }

    public Guid PickupLocationId { get; set; }

    public Guid DeliveryLocationId { get; set; }

    public DateTime PickupDate { get; set; }

    public DateTime? DeliveryDate { get; set; }
}