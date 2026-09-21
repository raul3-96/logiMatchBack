namespace LogiMatch.Domain.Entities;

public class Cargo
{
    public Guid Id { get; private set; }

    public Guid TransportRequestId { get; private set; }

    public string Description { get; private set; } = null!;

    public decimal WeightKg { get; private set; }

    public decimal VolumeM3 { get; private set; }

    public int Quantity { get; private set; }

    public bool RequiresRefrigeration { get; private set; }

    public bool RequiresTailLift { get; private set; }

    private Cargo()
    {
    }

    public Cargo(
        Guid transportRequestId,
        string description,
        decimal weightKg,
        decimal volumeM3,
        int quantity,
        bool requiresRefrigeration,
        bool requiresTailLift)
    {
        if (transportRequestId == Guid.Empty)
            throw new InvalidOperationException(
                "Transport request ID cannot be empty.");

        if (string.IsNullOrWhiteSpace(description))
            throw new InvalidOperationException(
                "Cargo description cannot be empty.");

        if (weightKg <= 0)
            throw new InvalidOperationException(
                "Cargo weight must be greater than zero.");

        if (volumeM3 <= 0)
            throw new InvalidOperationException(
                "Cargo volume must be greater than zero.");

        if (quantity <= 0)
            throw new InvalidOperationException(
                "Cargo quantity must be greater than zero.");

        Id = Guid.NewGuid();

        TransportRequestId = transportRequestId;
        Description = description.Trim();
        WeightKg = weightKg;
        VolumeM3 = volumeM3;
        Quantity = quantity;
        RequiresRefrigeration = requiresRefrigeration;
        RequiresTailLift = requiresTailLift;
    }
}