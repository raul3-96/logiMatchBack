namespace LogiMatch.Application.Authentication;

public record LoginUserResult(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string AccessToken);