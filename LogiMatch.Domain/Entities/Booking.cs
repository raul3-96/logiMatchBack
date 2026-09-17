using LogiMatch.Domain;
using LogiMatch.Domain.Enums;

namespace LogiMatch.Domain.Entities;

public class Booking
{
    public Guid Id { get; private set; }

    public Guid TransportRequestId { get; private set; }
    public Guid TransportOfferId { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public BookingStatus Status { get; private set; }

    private Booking()
    {
    }

    public Booking(
        Guid transportRequestId,
        Guid transportOfferId)
    {
        Id = Guid.NewGuid();
        TransportRequestId = transportRequestId;
        TransportOfferId = transportOfferId;
        CreatedAt = DateTime.UtcNow;
        Status = BookingStatus.Confirmed;
    }

    public void Start()
    {
        if (Status != BookingStatus.Confirmed)
            throw new InvalidOperationException(
                "Only confirmed bookings can be started.");

        Status = BookingStatus.InProgress;
    }

    public void Complete()
    {
        if (Status != BookingStatus.InProgress)
            throw new InvalidOperationException(
                "Only in-progress bookings can be completed.");

        Status = BookingStatus.Completed;
    }

    public void Cancel()
    {
        if (Status != BookingStatus.Confirmed)
            throw new InvalidOperationException(
                "Only confirmed bookings can be cancelled.");

        Status = BookingStatus.Cancelled;
    }
}