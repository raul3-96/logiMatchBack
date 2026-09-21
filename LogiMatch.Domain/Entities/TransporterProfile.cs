namespace LogiMatch.Domain.Entities;

public class TransporterProfile
{
    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid? CompanyId { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private TransporterProfile()
    {
    }

    public TransporterProfile(
        Guid userId,
        Guid? companyId = null)
    {
        if (userId == Guid.Empty)
            throw new InvalidOperationException(
                "User ID cannot be empty.");

        if (companyId.HasValue && companyId.Value == Guid.Empty)
            throw new InvalidOperationException(
                "Company ID cannot be empty.");

        Id = Guid.NewGuid();
        UserId = userId;
        CompanyId = companyId;
        CreatedAt = DateTime.UtcNow;
    }
}