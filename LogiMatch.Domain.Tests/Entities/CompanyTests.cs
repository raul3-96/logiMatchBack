using LogiMatch.Domain.Entities;
using Xunit;

namespace LogiMatch.Domain.Tests.Entities;

public class CompanyTests
{
    [Fact]
    public void Constructor_WhenValuesAreValid_ShouldCreateCompany()
    {
        // Arrange
        var name = "Transportes Example S.L.";
        var taxId = "B12345678";
        var email = "company@example.com";
        var phone = "600123456";

        // Act
        var company = new Company(
            name,
            taxId,
            email,
            phone);

        // Assert
        Assert.NotEqual(Guid.Empty, company.Id);
        Assert.Equal(name, company.Name);
        Assert.Equal(taxId, company.TaxId);
        Assert.Equal(email, company.Email);
        Assert.Equal(phone, company.Phone);
        Assert.NotEqual(default, company.CreatedAt);
    }

    [Fact]
    public void Constructor_ShouldTrimAndNormalizeValues()
    {
        // Arrange & Act
        var company = new Company(
            "  Transportes Example S.L.  ",
            "  b12345678  ",
            "  COMPANY@EXAMPLE.COM  ",
            "  600123456  ");

        // Assert
        Assert.Equal(
            "Transportes Example S.L.",
            company.Name);

        Assert.Equal(
            "B12345678",
            company.TaxId);

        Assert.Equal(
            "company@example.com",
            company.Email);

        Assert.Equal(
            "600123456",
            company.Phone);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenNameIsEmpty_ShouldThrow(string name)
    {
        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => new Company(
                name,
                "B12345678",
                "company@example.com",
                "600123456"));

        // Assert
        Assert.Equal(
            "Company name cannot be empty.",
            exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenTaxIdIsEmpty_ShouldThrow(string taxId)
    {
        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => new Company(
                "Transportes Example S.L.",
                taxId,
                "company@example.com",
                "600123456"));

        // Assert
        Assert.Equal(
            "Tax ID cannot be empty.",
            exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenEmailIsEmpty_ShouldThrow(string email)
    {
        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => new Company(
                "Transportes Example S.L.",
                "B12345678",
                email,
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
            () => new Company(
                "Transportes Example S.L.",
                "B12345678",
                email,
                "600123456"));

        // Assert
        Assert.Equal(
            "Email format is invalid.",
            exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenPhoneIsEmpty_ShouldThrow(string phone)
    {
        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => new Company(
                "Transportes Example S.L.",
                "B12345678",
                "company@example.com",
                phone));

        // Assert
        Assert.Equal(
            "Phone cannot be empty.",
            exception.Message);
    }
}