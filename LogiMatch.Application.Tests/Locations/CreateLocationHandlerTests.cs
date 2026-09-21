using LogiMatch.Application.Locations;
using LogiMatch.Application.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogiMatch.Application.Tests.Locations;

public class CreateLocationHandlerTests
{
    [Fact]
    public async Task Handle_WhenLocationIsValid_ShouldCreateLocation()
    {
        using var db = TestDbContextFactory.Create();
        var handler = new CreateLocationHandler(db);

        var command = new CreateLocationCommand
        {
            Address = "  Plaza de España 1  ",
            City = "  Sevilla  ",
            PostalCode = " 41013 ",
            Country = "  Spain  ",
            Latitude = 37.3772,
            Longitude = -5.9869
        };

        var locationId = await handler.Handle(command);

        var location = await db.Locations
            .SingleAsync(x => x.Id == locationId);

        Assert.NotEqual(Guid.Empty, locationId);
        Assert.Equal("Plaza de España 1", location.Address);
        Assert.Equal("Sevilla", location.City);
        Assert.Equal("41013", location.PostalCode);
        Assert.Equal("Spain", location.Country);
        Assert.Equal(37.3772, location.Latitude);
        Assert.Equal(-5.9869, location.Longitude);
    }

    [Fact]
    public async Task Handle_WhenAddressIsEmpty_ShouldThrow()
    {
        using var db = TestDbContextFactory.Create();
        var handler = new CreateLocationHandler(db);

        var command = new CreateLocationCommand
        {
            Address = "   ",
            City = "Sevilla",
            PostalCode = "41013",
            Country = "Spain",
            Latitude = 37.3772,
            Longitude = -5.9869
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command));

        Assert.Equal(
            "Address cannot be empty.",
            exception.Message);
    }
}