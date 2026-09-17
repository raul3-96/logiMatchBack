using LogiMatch.Domain.Entities;
using Xunit;

namespace LogiMatch.Domain.Tests.Entities;

public class TransporterProfileTests
{
    [Fact]
    public void Constructor_WhenUserIdIsValidAndCompanyIdIsNull_ShouldCreateProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var profile = new TransporterProfile(userId);

        // Assert
        Assert.NotEqual(Guid.Empty, profile.Id);
        Assert.Equal(userId, profile.UserId);
        Assert.Null(profile.CompanyId);
        Assert.NotEqual(default, profile.CreatedAt);
    }

    [Fact]
    public void Constructor_WhenUserIdAndCompanyIdAreValid_ShouldCreateProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();

        // Act
        var profile = new TransporterProfile(
            userId,
            companyId);

        // Assert
        Assert.NotEqual(Guid.Empty, profile.Id);
        Assert.Equal(userId, profile.UserId);
        Assert.Equal(companyId, profile.CompanyId);
        Assert.NotEqual(default, profile.CreatedAt);
    }

    [Fact]
    public void Constructor_WhenUserIdIsEmpty_ShouldThrow()
    {
        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => new TransporterProfile(Guid.Empty));

        // Assert
        Assert.Equal(
            "User ID cannot be empty.",
            exception.Message);
    }

    [Fact]
    public void Constructor_WhenCompanyIdIsEmpty_ShouldThrow()
    {
        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => new TransporterProfile(
                Guid.NewGuid(),
                Guid.Empty));

        // Assert
        Assert.Equal(
            "Company ID cannot be empty.",
            exception.Message);
    }
}