namespace LogiMatch.Application.Cargos;

public class CreateCargoCommand
{
    public Guid TransportRequestId { get; set; }

    public string Description { get; set; } = null!;

    public decimal WeightKg { get; set; }

    public decimal VolumeM3 { get; set; }

    public int Quantity { get; set; }

    public bool RequiresRefrigeration { get; set; }

    public bool RequiresTailLift { get; set; }
}