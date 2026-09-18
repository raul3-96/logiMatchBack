namespace LogiMatch.Application.Matching;

public class ReserveTripCapacityCommand
{
    public Guid TripId { get; set; }

    public Guid TransportRequestId { get; set; }
}