using LogiMatch.Application.Companies;
using LogiMatch.Application.Tests.Common;
using LogiMatch.Domain.Entities;
using LogiMatch.Domain.Enums;
using Xunit;

namespace LogiMatch.Application.Tests.Companies;

public class GetCompanyPrivateHandlerTests
{
    [Fact]
    public async Task Handle_WhenCompanyDoesNotExist_ShouldThrow()
    {
        using var db = TestDbContextFactory.Create();

        var handler = new GetCompanyPrivateHandler(
            db,
            new MockCurrentUserService(Guid.NewGuid()));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(Guid.NewGuid()));

        Assert.Equal(
            "The specified company does not exist.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenCurrentUserIsOwner_ShouldReturnPrivateCompany()
    {
        using var db = TestDbContextFactory.Create();

        var owner = CreateUser("owner@example.com");
        var company = CreateCompany(owner.Id);

        db.Users.Add(owner);
        db.Companies.Add(company);

        await db.SaveChangesAsync();

        var handler = new GetCompanyPrivateHandler(
            db,
            new MockCurrentUserService(owner.Id));

        var result = await handler.Handle(company.Id);

        Assert.NotNull(result);

        var resultType = result!.GetType();

        Assert.Equal(
            company.Id,
            resultType.GetProperty("Id")!.GetValue(result));

        Assert.Equal(
            company.Name,
            resultType.GetProperty("Name")!.GetValue(result));

        Assert.Equal(
            company.TaxId,
            resultType.GetProperty("TaxId")!.GetValue(result));

        Assert.Equal(
            company.Email,
            resultType.GetProperty("Email")!.GetValue(result));

        Assert.Equal(
            company.Phone,
            resultType.GetProperty("Phone")!.GetValue(result));

        Assert.Equal(
            company.OwnerUserId,
            resultType.GetProperty("OwnerUserId")!.GetValue(result));
    }

    [Fact]
    public async Task Handle_WhenCurrentUserIsAdmin_ShouldReturnPrivateCompany()
    {
        using var db = TestDbContextFactory.Create();

        var owner = CreateUser("owner@example.com");
        var admin = CreateUser("admin@example.com");
        var company = CreateCompany(owner.Id);

        var member = new CompanyMember(
            company.Id,
            admin.Id,
            CompanyMemberRole.Admin);

        db.Users.Add(owner);
        db.Users.Add(admin);
        db.Companies.Add(company);
        db.CompanyMembers.Add(member);

        await db.SaveChangesAsync();

        var handler = new GetCompanyPrivateHandler(
            db,
            new MockCurrentUserService(admin.Id));

        var result = await handler.Handle(company.Id);

        Assert.NotNull(result);

        var resultType = result!.GetType();

        Assert.Equal(
            company.Id,
            resultType.GetProperty("Id")!.GetValue(result));

        Assert.Equal(
            company.TaxId,
            resultType.GetProperty("TaxId")!.GetValue(result));

        Assert.Equal(
            company.Email,
            resultType.GetProperty("Email")!.GetValue(result));

        Assert.Equal(
            company.Phone,
            resultType.GetProperty("Phone")!.GetValue(result));
    }

    [Fact]
    public async Task Handle_WhenCurrentUserIsWorker_ShouldThrow()
    {
        using var db = TestDbContextFactory.Create();

        var owner = CreateUser("owner@example.com");
        var worker = CreateUser("worker@example.com");
        var company = CreateCompany(owner.Id);

        var member = new CompanyMember(
            company.Id,
            worker.Id,
            CompanyMemberRole.Worker);

        db.Users.Add(owner);
        db.Users.Add(worker);
        db.Companies.Add(company);
        db.CompanyMembers.Add(member);

        await db.SaveChangesAsync();

        var handler = new GetCompanyPrivateHandler(
            db,
            new MockCurrentUserService(worker.Id));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(company.Id));

        Assert.Equal(
            "Only the company owner or an administrator can access private company details.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenCurrentUserIsInactiveAdmin_ShouldThrow()
    {
        using var db = TestDbContextFactory.Create();

        var owner = CreateUser("owner@example.com");
        var admin = CreateUser("admin@example.com");
        var company = CreateCompany(owner.Id);

        var member = new CompanyMember(
            company.Id,
            admin.Id,
            CompanyMemberRole.Admin);

        member.Deactivate();

        db.Users.Add(owner);
        db.Users.Add(admin);
        db.Companies.Add(company);
        db.CompanyMembers.Add(member);

        await db.SaveChangesAsync();

        var handler = new GetCompanyPrivateHandler(
            db,
            new MockCurrentUserService(admin.Id));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(company.Id));

        Assert.Equal(
            "Only the company owner or an administrator can access private company details.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenCurrentUserIsNotRelatedToCompany_ShouldThrow()
    {
        using var db = TestDbContextFactory.Create();

        var owner = CreateUser("owner@example.com");
        var otherUser = CreateUser("other@example.com");
        var company = CreateCompany(owner.Id);

        db.Users.Add(owner);
        db.Users.Add(otherUser);
        db.Companies.Add(company);

        await db.SaveChangesAsync();

        var handler = new GetCompanyPrivateHandler(
            db,
            new MockCurrentUserService(otherUser.Id));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(company.Id));

        Assert.Equal(
            "Only the company owner or an administrator can access private company details.",
            exception.Message);
    }

    private static User CreateUser(
        string email)
    {
        return new User(
            email,
            "John",
            "Doe",
            "600123456");
    }

    private static Company CreateCompany(
        Guid ownerUserId)
    {
        return new Company(
            "LogiMatch Transport",
            "B12345678",
            "company@example.com",
            "600000000",
            ownerUserId);
    }
}