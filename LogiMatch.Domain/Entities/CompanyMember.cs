// LogiMatch.Domain/Entities/CompanyMember.cs
using LogiMatch.Domain.Enums;

namespace LogiMatch.Domain.Entities;

public class CompanyMember
{
    public Guid Id { get; private set; }

    public Guid CompanyId { get; private set; }

    public Guid UserId { get; private set; }

    public CompanyMemberRole Role { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private CompanyMember()
    {
    }

    public CompanyMember(
        Guid companyId,
        Guid userId,
        CompanyMemberRole role = CompanyMemberRole.Worker)
    {
        if (companyId == Guid.Empty)
            throw new InvalidOperationException("Company ID cannot be empty.");

        if (userId == Guid.Empty)
            throw new InvalidOperationException("User ID cannot be empty.");

        if (!Enum.IsDefined(role))
            throw new InvalidOperationException("The specified company member role is not valid.");

        Id = Guid.NewGuid();
        CompanyId = companyId;
        UserId = userId;
        Role = role;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }
}