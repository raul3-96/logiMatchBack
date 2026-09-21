namespace LogiMatch.Application.TransporterProfiles;

public class PublicTransporterProfileDto
{
    public Guid Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? CompanyName { get; set; }

    public DateTime CreatedAt { get; set; }
}