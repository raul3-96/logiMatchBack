using LogiMatch.Application.Common.Interfaces;

namespace LogiMatch.Application.Tests.Common;

public sealed class MockCurrentUserService : ICurrentUserService
{
    public MockCurrentUserService(Guid userId)
    {
        UserId = userId;
    }

    public Guid UserId { get; }
}