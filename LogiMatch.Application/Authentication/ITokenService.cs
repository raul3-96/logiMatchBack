using LogiMatch.Domain.Entities;

namespace LogiMatch.Application.Authentication;

public interface ITokenService
{
    string GenerateToken(User user);
}