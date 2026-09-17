namespace LogiMatch.Domain.Entities;

public class Location
{
    public Guid Id { get; private set; }

    public string Address { get; private set; } = null!;

    public string City { get; private set; } = null!;

    public string PostalCode { get; private set; } = null!;

    public string Country { get; private set; } = null!;

    public double Latitude { get; private set; }

    public double Longitude { get; private set; }

    private Location()
    {
    }

    public Location(
        string address,
        string city,
        string postalCode,
        string country,
        double latitude,
        double longitude)
    {
        if (string.IsNullOrWhiteSpace(address))
            throw new InvalidOperationException(
                "Address cannot be empty.");

        if (string.IsNullOrWhiteSpace(city))
            throw new InvalidOperationException(
                "City cannot be empty.");

        if (string.IsNullOrWhiteSpace(postalCode))
            throw new InvalidOperationException(
                "Postal code cannot be empty.");

        if (string.IsNullOrWhiteSpace(country))
            throw new InvalidOperationException(
                "Country cannot be empty.");

        if (double.IsNaN(latitude) || double.IsInfinity(latitude))
            throw new InvalidOperationException(
                "Latitude must be a valid number.");

        if (latitude < -90 || latitude > 90)
            throw new InvalidOperationException(
                "Latitude must be between -90 and 90.");

        if (double.IsNaN(longitude) || double.IsInfinity(longitude))
            throw new InvalidOperationException(
                "Longitude must be a valid number.");

        if (longitude < -180 || longitude > 180)
            throw new InvalidOperationException(
                "Longitude must be between -180 and 180.");

        Id = Guid.NewGuid();

        Address = address.Trim();
        City = city.Trim();
        PostalCode = postalCode.Trim();
        Country = country.Trim();
        Latitude = latitude;
        Longitude = longitude;
    }
}