namespace LogiMatch.Application.TransporterProfiles;

public class DetailedTransporterProfileDto
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? CompanyName { get; set; }

    public string? TaxId { get; set; }

    public DateTime CreatedAt { get; set; }
}