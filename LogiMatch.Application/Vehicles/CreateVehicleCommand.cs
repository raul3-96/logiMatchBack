using LogiMatch.Domain;
using LogiMatch.Domain.Enums;

namespace LogiMatch.Application.Vehicles;

public class CreateVehicleCommand
{
    public Guid TransporterProfileId { get; set; }

    public VehicleType Type { get; set; }

    public string Brand { get; set; } = null!;

    public string Model { get; set; } = null!;

    public string LicensePlate { get; set; } = null!;

    public decimal MaxWeightKg { get; set; }

    public decimal MaxVolumeM3 { get; set; }

    public decimal LengthM { get; set; }

    public decimal WidthM { get; set; }

    public decimal HeightM { get; set; }

    public bool HasTailLift { get; set; }

    public bool IsRefrigerated { get; set; }
}