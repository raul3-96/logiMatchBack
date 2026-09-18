using LogiMatch.Domain;
using LogiMatch.Domain.Entities;
using Xunit;

namespace LogiMatch.Domain.Tests.Entities;

public class UserTests
{
    [Fact]
    public void Constructor_WhenValuesAreValid_ShouldCreateActiveUser()
    {
        // Arrange
        var email = "user@example.com";
        var firstName = "John";
        var lastName = "Doe";
        var phone = "600123456";

        // Act
        var user = new User(
            email,
            firstName,
            lastName,
            phone);

        // Assert
        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal(email, user.Email);
        Assert.Equal(firstName, user.FirstName);
        Assert.Equal(lastName, user.LastName);
        Assert.Equal(phone, user.Phone);
        Assert.NotEqual(default, user.CreatedAt);
        Assert.Equal(UserStatus.Active, user.Status);
    }

    [Fact]
    public void Constructor_ShouldTrimAndNormalizeValues()
    {
        // Arrange
        var user = new User(
            "  USER@EXAMPLE.COM  ",
            "  John  ",
            "  Doe  ",
            "  600123456  ");

        // Act & Assert
        Assert.Equal("user@example.com", user.Email);
        Assert.Equal("John", user.FirstName);
        Assert.Equal("Doe", user.LastName);
        Assert.Equal("600123456", user.Phone);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenEmailIsEmpty_ShouldThrow(string email)
    {
        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => new User(
                email,
                "John",
                "Doe",
                "600123456"));

        // Assert
        Assert.Equal(
            "Email cannot be empty.",
            exception.Message);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("invalid@")]
    [InlineData("@example.com")]
    public void Constructor_WhenEmailFormatIsInvalid_ShouldThrow(string email)
    {
        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => new User(
                email,
                "John",
                "Doe",
                "600123456"));

        // Assert
        Assert.Equal(
            "Email format is invalid.",
            exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenFirstNameIsEmpty_ShouldThrow(string firstName)
    {
        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => new User(
                "user@example.com",
                firstName,
                "Doe",
                "600123456"));

        // Assert
        Assert.Equal(
            "First name cannot be empty.",
            exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenLastNameIsEmpty_ShouldThrow(string lastName)
    {
        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => new User(
                "user@example.com",
                "John",
                lastName,
                "600123456"));

        // Assert
        Assert.Equal(
            "Last name cannot be empty.",
            exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenPhoneIsEmpty_ShouldThrow(string phone)
    {
        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => new User(
                "user@example.com",
                "John",
                "Doe",
                phone));

        // Assert
        Assert.Equal(
            "Phone cannot be empty.",
            exception.Message);
    }
}