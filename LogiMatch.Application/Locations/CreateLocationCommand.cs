namespace LogiMatch.Application.Locations;

public class CreateLocationCommand
{
    public string Address { get; set; } = null!;

    public string City { get; set; } = null!;

    public string PostalCode { get; set; } = null!;

    public string Country { get; set; } = null!;

    public double Latitude { get; set; }

    public double Longitude { get; set; }
}