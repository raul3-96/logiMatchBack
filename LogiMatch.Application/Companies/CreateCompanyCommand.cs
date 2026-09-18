namespace LogiMatch.Application.Companies;

public record CreateCompanyCommand(
    string Name,
    string TaxId,
    string Email,
    string Phone);