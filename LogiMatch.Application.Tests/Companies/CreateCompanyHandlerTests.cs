using LogiMatch.Application.Companies;
using LogiMatch.Application.Tests.Common;
using LogiMatch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogiMatch.Application.Tests.Companies;

public class CreateCompanyHandlerTests
{
    [Fact]
    public async Task Handle_WhenTaxIdAlreadyExists_ShouldThrow()
    {
        using var db = TestDbContextFactory.Create();

        db.Companies.Add(CreateCompany(
            taxId: "B12345678"));

        await db.SaveChangesAsync();
        var handler = new CreateCompanyHandler(db, new MockCurrentUserService( Guid.NewGuid()));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(new CreateCompanyCommand(
                "Nueva empresa",
                "b12345678",
                "info@nuevaempresa.com",
                "600000000")));

        Assert.Equal(
            "A company with the specified Tax ID already exists.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenCompanyIsValid_ShouldCreateCompany()
    {
        using var db = TestDbContextFactory.Create();

        var handler = new CreateCompanyHandler(db, new MockCurrentUserService(Guid.NewGuid()));

        var command = new CreateCompanyCommand(
            "  LogiMatch SL  ",
            "  b12345678  ",
            "  INFO@LOGIMATCH.COM  ",
            "  600000000  ");

        var companyId = await handler.Handle(command);

        var company = await db.Companies
            .SingleAsync(x => x.Id == companyId);

        Assert.NotEqual(Guid.Empty, companyId);
        Assert.Equal("LogiMatch SL", company.Name);
        Assert.Equal("B12345678", company.TaxId);
        Assert.Equal("info@logimatch.com", company.Email);
        Assert.Equal("600000000", company.Phone);
    }

    private static Company CreateCompany(
        string name = "LogiMatch SL",
        string taxId = "B12345678",
        string email = "info@logimatch.com",
        string phone = "600000000")
    {
        return new Company(
            name,
            taxId,
            email,
            phone, Guid.NewGuid());
    }
}