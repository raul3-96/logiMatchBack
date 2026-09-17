using LogiMatch.Api.Common;
using LogiMatch.Application;
using LogiMatch.Application.Bookings;
using LogiMatch.Application.Cargos;
using LogiMatch.Application.Companies;
using LogiMatch.Application.Locations;
using LogiMatch.Application.Matching;
using LogiMatch.Application.TransporterProfiles;
using LogiMatch.Application.TransportOffers;
using LogiMatch.Application.TransportRequests;
using LogiMatch.Application.Trips;
using LogiMatch.Application.Users;
using LogiMatch.Application.VehicleAvailabilities;
using LogiMatch.Application.Vehicles;
using LogiMatch.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<LogiMatchDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IApplicationDbContext>(
    sp => sp.GetRequiredService<LogiMatchDbContext>());

builder.Services.AddScoped<CreateTransportRequestHandler>();
builder.Services.AddScoped<GetTransportRequestHandler>();
builder.Services.AddScoped<CreateUserHandler>();
builder.Services.AddScoped<CreateLocationHandler>();
builder.Services.AddScoped<CreateCargoHandler>();
builder.Services.AddScoped<StartTripCargoHandler>();
builder.Services.AddScoped<CompleteTripCargoHandler>();
builder.Services.AddScoped<CancelTripCargoHandler>();
builder.Services.AddScoped<GetTripHandler>();
builder.Services.AddScoped<CreateTransporterProfileHandler>();
builder.Services.AddScoped<CancelTransportRequestHandler>();
builder.Services.AddScoped<CreateVehicleHandler>();
builder.Services.AddScoped<CreateTransportOfferHandler>();
builder.Services.AddScoped<GetTransportOffersHandler>();
builder.Services.AddScoped<AcceptTransportOfferHandler>();
builder.Services.AddScoped<GetBookingHandler>();
builder.Services.AddScoped<StartBookingHandler>();
builder.Services.AddScoped<CompleteBookingHandler>();
builder.Services.AddScoped<CancelBookingHandler>();
builder.Services.AddScoped<PublishTransportRequestHandler>();
builder.Services.AddScoped<FindMatchingVehiclesHandler>();
builder.Services.AddScoped<CreateVehicleAvailabilityHandler>();
builder.Services.AddScoped<CreateTripHandler>();
builder.Services.AddScoped<StartTripHandler>();
builder.Services.AddScoped<CompleteTripHandler>();
builder.Services.AddScoped<CancelTripHandler>();
builder.Services.AddScoped<ReserveTripCapacityHandler>();
builder.Services.AddScoped<FindMatchingTripsHandler>();
builder.Services.AddScoped<CreateCompanyHandler>();

builder.Services.AddControllers();
builder.Services.AddProblemDetails();

builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseMiddleware<ApiExceptionMiddleware>();
app.MapControllers();

app.Run();