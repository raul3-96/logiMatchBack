using LogiMatch.Domain.Entities;
using Xunit;

namespace LogiMatch.Domain.Tests.Entities;

public class LocationTests
{
    [Fact]
    public void Constructor_WhenValuesAreValid_ShouldCreateLocation()
    {
        // Arrange
        var address = "Plaza de España 1";
        var city = "Sevilla";
        var postalCode = "41013";
        var country = "Spain";
        var latitude = 37.3772;
        var longitude = -5.9869;

        // Act
        var location = new Location(
            address,
            city,
            postalCode,
            country,
            latitude,
            longitude);

        // Assert
        Assert.NotEqual(Guid.Empty, location.Id);
        Assert.Equal(address, location.Address);
        Assert.Equal(city, location.City);
        Assert.Equal(postalCode, location.PostalCode);
        Assert.Equal(country, location.Country);
        Assert.Equal(latitude, location.Latitude);
        Assert.Equal(longitude, location.Longitude);
    }

    [Fact]
    public void Constructor_ShouldTrimTextValues()
    {
        // Arrange & Act
        var location = new Location(
            "  Plaza de España 1  ",
            "  Sevilla  ",
            " 41013 ",
            "  Spain  ",
            37.3772,
            -5.9869);

        // Assert
        Assert.Equal("Plaza de España 1", location.Address);
        Assert.Equal("Sevilla", location.City);
        Assert.Equal("41013", location.PostalCode);
        Assert.Equal("Spain", location.Country);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenAddressIsEmpty_ShouldThrow(string address)
    {
        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => new Location(
                address,
                "Sevilla",
                "41013",
                "Spain",
                37.3772,
                -5.9869));

        // Assert
        Assert.Equal(
            "Address cannot be empty.",
            exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenCityIsEmpty_ShouldThrow(string city)
    {
        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => new Location(
                "Plaza de España 1",
                city,
                "41013",
                "Spain",
                37.3772,
                -5.9869));

        // Assert
        Assert.Equal(
            "City cannot be empty.",
            exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenPostalCodeIsEmpty_ShouldThrow(string postalCode)
    {
        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => new Location(
                "Plaza de España 1",
                "Sevilla",
                postalCode,
                "Spain",
                37.3772,
                -5.9869));

        // Assert
        Assert.Equal(
            "Postal code cannot be empty.",
            exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenCountryIsEmpty_ShouldThrow(string country)
    {
        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => new Location(
                "Plaza de España 1",
                "Sevilla",
                "41013",
                country,
                37.3772,
                -5.9869));

        // Assert
        Assert.Equal(
            "Country cannot be empty.",
            exception.Message);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Constructor_WhenLatitudeIsNotFinite_ShouldThrow(double latitude)
    {
        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => new Location(
                "Plaza de España 1",
                "Sevilla",
                "41013",
                "Spain",
                latitude,
                -5.9869));

        // Assert
        Assert.Equal(
            "Latitude must be a valid number.",
            exception.Message);
    }

    [Theory]
    [InlineData(-90.0001)]
    [InlineData(90.0001)]
    public void Constructor_WhenLatitudeIsOutOfRange_ShouldThrow(double latitude)
    {
        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => new Location(
                "Plaza de España 1",
                "Sevilla",
                "41013",
                "Spain",
                latitude,
                -5.9869));

        // Assert
        Assert.Equal(
            "Latitude must be between -90 and 90.",
            exception.Message);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Constructor_WhenLongitudeIsNotFinite_ShouldThrow(double longitude)
    {
        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => new Location(
                "Plaza de España 1",
                "Sevilla",
                "41013",
                "Spain",
                37.3772,
                longitude));

        // Assert
        Assert.Equal(
            "Longitude must be a valid number.",
            exception.Message);
    }

    [Theory]
    [InlineData(-180.0001)]
    [InlineData(180.0001)]
    public void Constructor_WhenLongitudeIsOutOfRange_ShouldThrow(double longitude)
    {
        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => new Location(
                "Plaza de España 1",
                "Sevilla",
                "41013",
                "Spain",
                37.3772,
                longitude));

        // Assert
        Assert.Equal(
            "Longitude must be between -180 and 180.",
            exception.Message);
    }

    [Theory]
    [InlineData(-90)]
    [InlineData(90)]
    public void Constructor_WhenLatitudeIsAtBoundary_ShouldCreateLocation(
        double latitude)
    {
        // Act
        var location = new Location(
            "Plaza de España 1",
            "Sevilla",
            "41013",
            "Spain",
            latitude,
            -5.9869);

        // Assert
        Assert.Equal(latitude, location.Latitude);
    }

    [Theory]
    [InlineData(-180)]
    [InlineData(180)]
    public void Constructor_WhenLongitudeIsAtBoundary_ShouldCreateLocation(
        double longitude)
    {
        // Act
        var location = new Location(
            "Plaza de España 1",
            "Sevilla",
            "41013",
            "Spain",
            37.3772,
            longitude);

        // Assert
        Assert.Equal(longitude, location.Longitude);
    }
}