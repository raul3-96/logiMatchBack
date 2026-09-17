namespace LogiMatch.Application.TransporterProfiles;

public class CreateTransporterProfileCommand
{
    public Guid UserId { get; set; }

    public Guid? CompanyId { get; set; }
}