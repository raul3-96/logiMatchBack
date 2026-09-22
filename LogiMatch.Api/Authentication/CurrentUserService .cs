using LogiMatch.Application.Common.Exceptions;
using LogiMatch.Application.Common.Interfaces;
using System.Security.Claims;

namespace LogiMatch.Api.Authentication;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(
        IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId
    {
        get
        {
            var userId = _httpContextAccessor.HttpContext?
                .User
                .FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userId, out var id))
                throw new UnauthorizedException(
                    "Authenticated user ID is missing.");

            return id;
        }
    }
}