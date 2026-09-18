using LogiMatch.Application.Common.Exceptions;
using LogiMatch.Domain.Entities;
using LogiMatch.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.TransportOffers;

public class AcceptTransportOfferHandler
{
    private readonly IApplicationDbContext _dbContext;

    public AcceptTransportOfferHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid> Handle(Guid offerId)
    {
        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync();

        try
        {
            // 1. Buscar la oferta
            var offer = await _dbContext.TransportOffers
                .FirstOrDefaultAsync(x => x.Id == offerId);

            if (offer == null)
                throw new NotFoundException(
                    "The specified transport offer does not exist.");

            // 2. Comprobar que está pendiente
            if (offer.Status != TransportOfferStatus.Pending)
                throw new ConflictException(
                    "Only pending offers can be accepted.");

            // 3. Buscar la solicitud
            var request = await _dbContext.TransportRequests
                .FirstOrDefaultAsync(x => x.Id == offer.TransportRequestId);

            if (request == null)
                throw new NotFoundException(
                    "The transport request associated with the offer does not exist.");

            // 4. Comprobar que el vehículo pertenece al transportista
            var vehicle = await _dbContext.Vehicles
                .FirstOrDefaultAsync(x => x.Id == offer.VehicleId);

            if (vehicle == null)
                throw new NotFoundException(
                    "The vehicle associated with the offer does not exist.");

            if (vehicle.TransporterProfileId != offer.TransporterProfileId)
                throw new ConflictException(
                    "The vehicle does not belong to the transporter who created the offer.");

            // 5. Comprobar estado de la solicitud
            if (request.Status != TransportRequestStatus.OffersReceived)
                throw new ConflictException(
                    "Only requests with received offers can accept an offer.");

            // 6. Comprobar que la solicitud no tiene un Booking activo
            var activeBookingExists = await _dbContext.Bookings
                .AnyAsync(x =>
                    x.TransportRequestId == offer.TransportRequestId &&
                    x.Status != BookingStatus.Cancelled);

            if (activeBookingExists)
                throw new ConflictException(
                    "The transport request already has an active booking.");

            // 7. Comprobar que la solicitud no está asignada a un Trip
            var tripCargoExists = await _dbContext.TripCargos
                .AnyAsync(x =>
                    x.TransportRequestId == offer.TransportRequestId &&
                    x.Status != TripCargoStatus.Cancelled);

            if (tripCargoExists)
                throw new ConflictException(
                    "The transport request is already assigned to a trip.");

            // 8. Revalidar disponibilidad del vehículo
            var vehicleAvailable = await _dbContext.VehicleAvailabilities
                .AnyAsync(x =>
                    x.VehicleId == offer.VehicleId &&
                    x.AvailableFrom <= offer.EstimatedPickupDate &&
                    x.AvailableTo >= offer.EstimatedDeliveryDate);

            if (!vehicleAvailable)
                throw new ConflictException(
                    "The vehicle is no longer available for the offer dates.");

            // 9. Comprobar que el vehículo no está comprometido
            //    en otro Booking con fechas solapadas
            var vehicleHasOverlappingBooking =
                await _dbContext.Bookings
                    .Join(
                        _dbContext.TransportOffers,
                        booking => booking.TransportOfferId,
                        otherOffer => otherOffer.Id,
                        (booking, otherOffer) => new
                        {
                            Booking = booking,
                            Offer = otherOffer
                        })
                    .AnyAsync(x =>
                        x.Booking.Status != BookingStatus.Cancelled &&
                        x.Offer.VehicleId == offer.VehicleId &&
                        x.Offer.EstimatedPickupDate < offer.EstimatedDeliveryDate &&
                        x.Offer.EstimatedDeliveryDate > offer.EstimatedPickupDate);

            if (vehicleHasOverlappingBooking)
                throw new ConflictException(
                    "The vehicle is already committed to another booking that overlaps with the specified offer dates.");

            // 10. Comprobar que el vehículo no tiene otro Trip
            //    con fechas solapadas
            var vehicleHasOverlappingTrip =
                await _dbContext.Trips
                    .AnyAsync(x =>
                        x.VehicleId == offer.VehicleId &&
                        x.Status != TripStatus.Cancelled &&
                        offer.EstimatedPickupDate < x.EstimatedArrivalDate &&
                        offer.EstimatedDeliveryDate > x.DepartureDate);

            if (vehicleHasOverlappingTrip)
                throw new ConflictException(
                    "The vehicle already has a trip that overlaps with the specified offer dates.");

            // 11. Buscar las demás ofertas pendientes
            var otherOffers = await _dbContext.TransportOffers
                .Where(x =>
                    x.TransportRequestId == offer.TransportRequestId &&
                    x.Id != offer.Id &&
                    x.Status == TransportOfferStatus.Pending)
                .ToListAsync();

            // 12. Aceptar la oferta
            offer.Accept();

            // 13. Rechazar las demás ofertas
            foreach (var otherOffer in otherOffers)
            {
                otherOffer.Reject();
            }

            // 14. Aceptar la solicitud
            request.Accept();

            // 15. Crear el Booking
            var booking = new Booking(
                offer.TransportRequestId,
                offer.Id);

            _dbContext.Bookings.Add(booking);

            // 16. Guardar todo
            await _dbContext.SaveChangesAsync();

            await transaction.CommitAsync();

            return booking.Id;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}