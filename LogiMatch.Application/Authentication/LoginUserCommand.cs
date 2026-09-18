namespace LogiMatch.Application.Authentication;

public record LoginUserCommand(
    string Email,
    string Password);