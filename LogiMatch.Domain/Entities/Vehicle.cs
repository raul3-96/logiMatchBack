using LogiMatch.Domain;
using LogiMatch.Domain.Enums;

namespace LogiMatch.Domain.Entities;

public class Vehicle
{
    public Guid Id { get; private set; }

    public Guid TransporterProfileId { get; private set; }

    public VehicleType Type { get; private set; }

    public string Brand { get; private set; } = null!;

    public string Model { get; private set; } = null!;

    public string LicensePlate { get; private set; } = null!;

    public decimal MaxWeightKg { get; private set; }

    public decimal MaxVolumeM3 { get; private set; }

    public decimal LengthM { get; private set; }

    public decimal WidthM { get; private set; }

    public decimal HeightM { get; private set; }

    public bool HasTailLift { get; private set; }

    public bool IsRefrigerated { get; private set; }

    private Vehicle()
    {
    }

    public Vehicle(
        Guid transporterProfileId,
        VehicleType type,
        string brand,
        string model,
        string licensePlate,
        decimal maxWeightKg,
        decimal maxVolumeM3,
        decimal lengthM,
        decimal widthM,
        decimal heightM,
        bool hasTailLift,
        bool isRefrigerated)
    {
        if (transporterProfileId == Guid.Empty)
            throw new InvalidOperationException(
                "Transporter profile ID cannot be empty.");

        if (!Enum.IsDefined(type))
            throw new InvalidOperationException(
                "The specified vehicle type is not valid.");

        if (string.IsNullOrWhiteSpace(brand))
            throw new InvalidOperationException(
                "Vehicle brand cannot be empty.");

        if (string.IsNullOrWhiteSpace(model))
            throw new InvalidOperationException(
                "Vehicle model cannot be empty.");

        if (string.IsNullOrWhiteSpace(licensePlate))
            throw new InvalidOperationException(
                "Vehicle license plate cannot be empty.");

        if (maxWeightKg <= 0)
            throw new InvalidOperationException(
                "Maximum weight must be greater than zero.");

        if (maxVolumeM3 <= 0)
            throw new InvalidOperationException(
                "Maximum volume must be greater than zero.");

        if (lengthM <= 0)
            throw new InvalidOperationException(
                "Vehicle length must be greater than zero.");

        if (widthM <= 0)
            throw new InvalidOperationException(
                "Vehicle width must be greater than zero.");

        if (heightM <= 0)
            throw new InvalidOperationException(
                "Vehicle height must be greater than zero.");

        Id = Guid.NewGuid();

        TransporterProfileId = transporterProfileId;
        Type = type;
        Brand = brand.Trim();
        Model = model.Trim();
        LicensePlate = licensePlate.Trim().ToUpperInvariant();
        MaxWeightKg = maxWeightKg;
        MaxVolumeM3 = maxVolumeM3;
        LengthM = lengthM;
        WidthM = widthM;
        HeightM = heightM;
        HasTailLift = hasTailLift;
        IsRefrigerated = isRefrigerated;
    }
}