using LogiMatch.Domain;
using LogiMatch.Domain.Enums;

namespace LogiMatch.Domain.Entities;

public class TransportRequest
{
    public Guid Id { get; private set; }

    public Guid CustomerId { get; private set; }

    public Guid PickupLocationId { get; private set; }

    public Guid DeliveryLocationId { get; private set; }

    public DateTime PickupDate { get; private set; }

    public DateTime? DeliveryDate { get; private set; }

    public TransportRequestStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public List<Cargo> Cargos { get; private set; } = new();
    public FulfillmentMode? Fulfillment { get; private set; }

    private TransportRequest()
    {
    }

    public TransportRequest(
        Guid customerId,
        Guid pickupLocationId,
        Guid deliveryLocationId,
        DateTime pickupDate,
        DateTime? deliveryDate)
    {
        if (deliveryDate.HasValue && deliveryDate.Value < pickupDate)
            throw new InvalidOperationException(
                "Delivery date cannot be before pickup date.");

        Id = Guid.NewGuid();

        CustomerId = customerId;
        PickupLocationId = pickupLocationId;
        DeliveryLocationId = deliveryLocationId;

        PickupDate = pickupDate;
        DeliveryDate = deliveryDate;

        Status = TransportRequestStatus.Draft;

        CreatedAt = DateTime.UtcNow;
    }

    public void Publish()
    {
        if (Status != TransportRequestStatus.Draft)
            throw new InvalidOperationException(
                "Only draft requests can be published.");

        Status = TransportRequestStatus.Published;
    }

    public void StartMatching()
    {
        if (Status != TransportRequestStatus.Published)
            throw new InvalidOperationException(
                "Only published requests can start matching.");

        Status = TransportRequestStatus.Matching;
    }

    public void MarkOffersReceived()
    {
        if (Status != TransportRequestStatus.Published &&
            Status != TransportRequestStatus.Matching)
            throw new InvalidOperationException(
                "Offers can only be received for published or matching requests.");

        Status = TransportRequestStatus.OffersReceived;
    }

    public void Accept()
    {
        if (Status != TransportRequestStatus.OffersReceived)
            throw new InvalidOperationException(
                "Only requests with received offers can be accepted.");

        Status = TransportRequestStatus.Accepted;
        Fulfillment = FulfillmentMode.Offer;
    }

    public void AssignToTrip()
    {
        if (Status != TransportRequestStatus.Published &&
            Status != TransportRequestStatus.Matching)
            throw new InvalidOperationException(
                "Only published or matching requests can be assigned to a trip.");

        Status = TransportRequestStatus.Accepted;
        Fulfillment = FulfillmentMode.Trip;
    }

    public void Start()
    {
        if (Status != TransportRequestStatus.Accepted)
            throw new InvalidOperationException(
                "Only accepted requests can be started.");

        Status = TransportRequestStatus.InProgress;
    }

    public void Complete()
    {
        if (Status != TransportRequestStatus.InProgress)
            throw new InvalidOperationException(
                "Only in-progress requests can be completed.");

        Status = TransportRequestStatus.Completed;
    }

    public void Cancel()
    {
        if (Status == TransportRequestStatus.InProgress)
            throw new InvalidOperationException(
                "In-progress requests cannot be cancelled.");

        if (Status == TransportRequestStatus.Completed)
            throw new InvalidOperationException(
                "Completed requests cannot be cancelled.");

        if (Status == TransportRequestStatus.Cancelled)
            throw new InvalidOperationException(
                "The request is already cancelled.");

        if (Status == TransportRequestStatus.Expired)
            throw new InvalidOperationException(
                "Expired requests cannot be cancelled.");

        Status = TransportRequestStatus.Cancelled;
    }

    public void Expire()
    {
        if (Status != TransportRequestStatus.Published &&
            Status != TransportRequestStatus.Matching &&
            Status != TransportRequestStatus.OffersReceived)
            throw new InvalidOperationException(
                "Only active requests can expire.");

        Status = TransportRequestStatus.Expired;
    }
    public void ReturnToPublished()
    {
        if (Status != TransportRequestStatus.Accepted)
            throw new InvalidOperationException(
                "Only accepted requests can be returned to published.");

        Status = TransportRequestStatus.Published;
        Fulfillment = null;
    }
}