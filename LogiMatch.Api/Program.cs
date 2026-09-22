using LogiMatch.Api.Authentication;
using LogiMatch.Api.Common;
using LogiMatch.Application;
using LogiMatch.Application.Authentication;
using LogiMatch.Application.Bookings;
using LogiMatch.Application.Cargos;
using LogiMatch.Application.Common.Interfaces;
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
using LogiMatch.Domain.Entities;
using LogiMatch.Infrastructure.Persistence.Configurations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("Jwt"));

var jwtSettings = builder.Configuration
    .GetSection("Jwt")
    .Get<JwtSettings>()
    ?? throw new InvalidOperationException("JWT configuration is missing.");

if (string.IsNullOrWhiteSpace(jwtSettings.Issuer))
    throw new InvalidOperationException("JWT Issuer is missing.");

if (string.IsNullOrWhiteSpace(jwtSettings.Audience))
    throw new InvalidOperationException("JWT Audience is missing.");

if (string.IsNullOrWhiteSpace(jwtSettings.SecretKey))
    throw new InvalidOperationException("JWT SecretKey is missing.");

if (jwtSettings.ExpirationMinutes <= 0)
    throw new InvalidOperationException("JWT ExpirationMinutes is missing.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddDbContext<LogiMatchDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IApplicationDbContext>(
    sp => sp.GetRequiredService<LogiMatchDbContext>());

builder.Services.AddScoped<CreateTransportRequestHandler>();
builder.Services.AddScoped<GetTransportRequestHandler>();
builder.Services.AddScoped<CreateUserHandler>();
builder.Services.AddScoped<GetUserHandler>();
builder.Services.AddScoped<LoginUserHandler>();
builder.Services.AddScoped<CreateLocationHandler>();
builder.Services.AddScoped<GetLocationHandler>();
builder.Services.AddScoped<CreateCargoHandler>();
builder.Services.AddScoped<GetTripCargoHandler>();
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
builder.Services.AddScoped<RejectTransportOfferHandler>();
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
builder.Services.AddScoped<AddCompanyMemberHandler>();
builder.Services.AddScoped<PasswordHasher<User>>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();

builder.Services.AddControllers();
builder.Services.AddProblemDetails();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "LogiMatch API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Introduce el token JWT así: Bearer {token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<ApiExceptionMiddleware>();
app.MapControllers();

app.Run();