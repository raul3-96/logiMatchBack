using LogiMatch.Domain;
using LogiMatch.Domain.Enums;

namespace LogiMatch.Domain.Entities;

public class TransportOffer
{
    public Guid Id { get; private set; }

    public Guid TransportRequestId { get; private set; }

    public Guid TransporterProfileId { get; private set; }

    public Guid VehicleId { get; private set; }

    public decimal Price { get; private set; }

    public DateTime EstimatedPickupDate { get; private set; }

    public DateTime EstimatedDeliveryDate { get; private set; }

    public TransportOfferStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private TransportOffer()
    {
    }

    public TransportOffer(
        Guid transportRequestId,
        Guid transporterProfileId,
        Guid vehicleId,
        decimal price,
        DateTime estimatedPickupDate,
        DateTime estimatedDeliveryDate)
    {
        Id = Guid.NewGuid();

        TransportRequestId = transportRequestId;
        TransporterProfileId = transporterProfileId;
        VehicleId = vehicleId;

        Price = price;

        EstimatedPickupDate = estimatedPickupDate;
        EstimatedDeliveryDate = estimatedDeliveryDate;

        Status = TransportOfferStatus.Pending;

        CreatedAt = DateTime.UtcNow;
    }

    public void Accept()
    {
        if (Status != TransportOfferStatus.Pending)
            throw new InvalidOperationException(
                "Only pending offers can be accepted.");

        Status = TransportOfferStatus.Accepted;
    }

    public void Reject()
    {
        if (Status != TransportOfferStatus.Pending)
            throw new InvalidOperationException(
                "Only pending offers can be rejected.");

        Status = TransportOfferStatus.Rejected;
    }

    public void Cancel()
    {
        if (Status != TransportOfferStatus.Pending)
            throw new InvalidOperationException(
                "Only pending offers can be cancelled.");

        Status = TransportOfferStatus.Cancelled;
    }
}